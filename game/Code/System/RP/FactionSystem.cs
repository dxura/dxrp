using Dxura.RP.Shared;
using Sandbox.Diagnostics;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public struct FactionInfo
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string Tag { get; set; }
	public string? Description { get; set; }
	public uint Balance { get; set; }
	public uint Level { get; set; }
	public uint Experience { get; set; }
	public uint MaxMembers { get; set; }
	public int MemberCount { get; set; }
}

public struct FactionRoleInfo
{
	public Guid Id { get; set; }
	public Guid FactionId { get; set; }
	public string Name { get; set; }
	public string? Description { get; set; }
	public int Order { get; set; }
	public FactionPermission Permission { get; set; }
}

public class FactionSystem : SingletonComponent<FactionSystem>
{
	[Sync( SyncFlags.FromHost )] public NetDictionary<Guid, FactionInfo> Factions { get; set; } = new();
	[Sync( SyncFlags.FromHost )] public NetDictionary<Guid, FactionRoleInfo> FactionRoles { get; set; } = new();

	protected override void OnStart()
	{
		if ( !Config.Current.Game.FactionsEnabled )
		{
			Destroy();
			return;
		}

		if ( Networking.IsHost )
		{
			_ = LoadFactions();
		}
	}

	private async Task LoadFactions()
	{
		var factions = await ServerApiClient.GetAllFactions();
		if ( factions == null )
		{
			return;
		}

		await GameTask.MainThread();

		foreach ( var faction in factions )
		{
			Factions[faction.Id] = new FactionInfo
			{
				Id = faction.Id,
				Name = faction.Name,
				Tag = faction.Tag,
				Description = faction.Description,
				Balance = faction.Balance,
				Level = faction.Level,
				Experience = faction.Experience,
				MaxMembers = faction.MaxMembers,
				MemberCount = faction.MemberCount
			};

			foreach ( var role in faction.Roles )
			{
				FactionRoles[role.Id] = new FactionRoleInfo
				{
					Id = role.Id,
					FactionId = faction.Id,
					Name = role.Name,
					Description = role.Description,
					Order = role.Order,
					Permission = role.Permission
				};
			}
		}
	}

	public IEnumerable<FactionRoleInfo> GetFactionRoles( Guid factionId )
	{
		return FactionRoles.Values.Where( r => r.FactionId == factionId );
	}

	private bool HasFactionPermission( Player player, Guid factionId, FactionPermission permission )
	{
		if ( !player.IsInFaction || player.FactionId != factionId )
		{
			return false;
		}

		var role = player.GetFactionRole();
		return role != null && role.Value.Permission.HasFlag( permission );
	}

	public async Task RefreshFaction( Guid factionId )
	{
		Assert.True( Networking.IsHost );

		var faction = await ServerApiClient.GetFaction( factionId );
		if ( faction == null )
		{
			return;
		}

		await GameTask.MainThread();

		Factions[faction.Id] = new FactionInfo
		{
			Id = faction.Id,
			Name = faction.Name,
			Tag = faction.Tag,
			Description = faction.Description,
			Balance = faction.Balance,
			Level = faction.Level,
			Experience = faction.Experience,
			MaxMembers = faction.MaxMembers,
			MemberCount = faction.MemberCount
		};

		// Remove old roles for this faction
		var oldRoleIds = FactionRoles.Where( r => r.Value.FactionId == factionId ).Select( r => r.Key ).ToList();
		foreach ( var roleId in oldRoleIds )
		{
			FactionRoles.Remove( roleId );
		}

		// Add updated roles
		foreach ( var role in faction.Roles )
		{
			FactionRoles[role.Id] = new FactionRoleInfo
			{
				Id = role.Id,
				FactionId = faction.Id,
				Name = role.Name,
				Description = role.Description,
				Order = role.Order,
				Permission = role.Permission
			};
		}
	}

	[Rpc.Host]
	public void CreateFactionHost( string name, string tag, string? description )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:faction:create", Config.Current.Game.ActionLongCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );
		if ( caller == null )
		{
			return;
		}

		if ( caller.IsInFaction )
		{
			caller.Error( "You are already in a faction" );
			return;
		}

		var cost = Config.Current.Game.FactionCreateCost;

		_ = GameTask.RunInThreadAsync( async () =>
		{
			if ( !await caller.ChargeHost( cost, "Created a faction" ) )
			{
				return;
			}

			var faction = await ServerApiClient.CreateFaction( new CreateFactionDto
			{
				Name = name, Tag = tag, Description = description
			} );

			if ( faction == null )
			{
				Log.Warning( $"[Faction] Failed to create faction '{name}' [{tag}] for {caller.DisplayName} ({caller.SteamId}) - API returned null" );
				await caller.PayHost( cost, "Faction creation refund" );
				caller.Error( "Failed to create faction" );
				return;
			}

			Log.Info( $"[Faction] {caller.DisplayName} ({caller.SteamId}) created faction '{faction.Name}' [{faction.Tag}] (ID: {faction.Id})" );

			// Create a Leader role with all permissions
			var leaderRole = await ServerApiClient.CreateFactionRole( faction.Id, new CreateFactionRoleDto
			{
				Name = "Leader", Order = 0, Permission = FactionPermission.InviteMember | FactionPermission.KickMember | FactionPermission.ManageFaction | FactionPermission.SetRanks | FactionPermission.WithdrawMoney
			} );

			// Add the creator as a member
			await ServerApiClient.AddFactionMember( faction.Id, new AddFactionMemberDto
			{
				PlayerId = caller.SteamId, RoleId = leaderRole?.Id
			} );

			await GameTask.MainThread();

			Factions[faction.Id] = new FactionInfo
			{
				Id = faction.Id,
				Name = faction.Name,
				Tag = faction.Tag,
				Description = faction.Description,
				Balance = faction.Balance,
				Level = faction.Level,
				Experience = faction.Experience,
				MaxMembers = faction.MaxMembers,
				MemberCount = 1
			};

			if ( leaderRole != null )
			{
				FactionRoles[leaderRole.Id] = new FactionRoleInfo
				{
					Id = leaderRole.Id,
					FactionId = faction.Id,
					Name = leaderRole.Name,
					Description = leaderRole.Description,
					Order = leaderRole.Order,
					Permission = leaderRole.Permission
				};
			}

			caller.FactionId = faction.Id;
			caller.FactionRoleId = leaderRole?.Id;

			Chat.Current.BroadcastSystemText( $"{caller.DisplayName} has created the faction {faction.Name} [{faction.Tag}]!" );
		} );
	}

	[Rpc.Host]
	public void UpdateFactionHost( Guid factionId, string? description )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:faction:update", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );
		if ( caller == null )
		{
			return;
		}

		if ( !HasFactionPermission( caller, factionId, FactionPermission.ManageFaction ) )
		{
			caller.Error( "No permission" );
			return;
		}

		_ = GameTask.RunInThreadAsync( async () =>
		{
			var faction = await ServerApiClient.UpdateFaction( factionId, new UpdateFactionDto
			{
				Description = description
			} );

			if ( faction == null )
			{
				await GameTask.MainThread();
				caller.Error( "Failed to update faction" );
				return;
			}

			await GameTask.MainThread();

			Factions[faction.Id] = new FactionInfo
			{
				Id = faction.Id,
				Name = faction.Name,
				Tag = faction.Tag,
				Description = faction.Description,
				Balance = faction.Balance,
				Level = faction.Level,
				Experience = faction.Experience,
				MaxMembers = faction.MaxMembers,
				MemberCount = faction.MemberCount
			};
		} );
	}

	[Rpc.Host]
	public void DeleteFactionHost( Guid factionId )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:faction:delete", Config.Current.Game.ActionLongCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );
		if ( caller == null )
		{
			return;
		}

		if ( !HasFactionPermission( caller, factionId, FactionPermission.ManageFaction ) )
		{
			caller.Error( "No permission" );
			return;
		}

		_ = GameTask.RunInThreadAsync( async () =>
		{
			await ServerApiClient.DeleteFaction( factionId );

			await GameTask.MainThread();

			Factions.Remove( factionId );

			var roleIds = FactionRoles.Where( r => r.Value.FactionId == factionId ).Select( r => r.Key ).ToList();
			foreach ( var roleId in roleIds )
			{
				FactionRoles.Remove( roleId );
			}
		} );
	}

	[Rpc.Host]
	public void CreateFactionRoleHost( Guid factionId, string name, string? description, int order, FactionPermission permission )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:faction:role:create", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );
		if ( caller == null )
		{
			return;
		}

		if ( !HasFactionPermission( caller, factionId, FactionPermission.SetRanks ) )
		{
			caller.Error( "No permission" );
			return;
		}

		_ = GameTask.RunInThreadAsync( async () =>
		{
			var role = await ServerApiClient.CreateFactionRole( factionId, new CreateFactionRoleDto
			{
				Name = name, Description = description, Order = order, Permission = permission
			} );

			if ( role == null )
			{
				await GameTask.MainThread();
				caller.Error( "Failed to create role" );
				return;
			}

			await GameTask.MainThread();

			FactionRoles[role.Id] = new FactionRoleInfo
			{
				Id = role.Id,
				FactionId = factionId,
				Name = role.Name,
				Description = role.Description,
				Order = role.Order,
				Permission = role.Permission
			};
		} );
	}

	[Rpc.Host]
	public void UpdateFactionRoleHost( Guid factionId, Guid roleId, string? name, string? description, int? order, FactionPermission? permission )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:faction:role:update", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );
		if ( caller == null )
		{
			return;
		}

		if ( !HasFactionPermission( caller, factionId, FactionPermission.SetRanks ) )
		{
			caller.Error( "No permission" );
			return;
		}

		_ = GameTask.RunInThreadAsync( async () =>
		{
			var role = await ServerApiClient.UpdateFactionRole( factionId, roleId, new UpdateFactionRoleDto
			{
				Name = name, Description = description, Order = order, Permission = permission
			} );

			if ( role == null )
			{
				await GameTask.MainThread();
				caller.Error( "Failed to update role" );
				return;
			}

			await GameTask.MainThread();

			FactionRoles[role.Id] = new FactionRoleInfo
			{
				Id = role.Id,
				FactionId = factionId,
				Name = role.Name,
				Description = role.Description,
				Order = role.Order,
				Permission = role.Permission
			};
		} );
	}

	[Rpc.Host]
	public void InviteFactionMemberHost( Guid factionId, long targetSteamId )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:faction:invite", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );
		if ( caller == null )
		{
			return;
		}

		if ( !HasFactionPermission( caller, factionId, FactionPermission.InviteMember ) )
		{
			caller.Error( "No permission" );
			return;
		}

		var target = GameUtils.GetPlayerById( targetSteamId );
		if ( target == null || target.IsInFaction )
		{
			caller.Error( "Player not found or already in a faction" );
			return;
		}

		_ = GameTask.RunInThreadAsync( async () =>
		{
			await ServerApiClient.AddFactionMember( factionId, new AddFactionMemberDto
			{
				PlayerId = targetSteamId
			} );

			await GameTask.MainThread();

			target.FactionId = factionId;
			await RefreshFaction( factionId );
		} );
	}

	[Rpc.Host]
	public void KickFactionMemberHost( Guid factionId, long targetSteamId )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:faction:kick", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );
		if ( caller == null )
		{
			return;
		}

		if ( !HasFactionPermission( caller, factionId, FactionPermission.KickMember ) )
		{
			caller.Error( "No permission" );
			return;
		}

		// Can't kick yourself
		if ( caller.SteamId == targetSteamId )
		{
			return;
		}

		var target = GameUtils.GetPlayerById( targetSteamId );

		_ = GameTask.RunInThreadAsync( async () =>
		{
			await ServerApiClient.RemoveFactionMember( factionId, targetSteamId );

			await GameTask.MainThread();

			if ( target != null && target.IsValid() )
			{
				target.FactionId = null;
				target.FactionRoleId = null;
			}

			await RefreshFaction( factionId );
		} );
	}

	[Rpc.Host]
	public void LeaveFactionHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:faction:leave", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );
		if ( caller == null || !caller.IsInFaction )
		{
			return;
		}

		var factionId = caller.FactionId!.Value;

		_ = GameTask.RunInThreadAsync( async () =>
		{
			await ServerApiClient.RemoveFactionMember( factionId, caller.SteamId );

			await GameTask.MainThread();

			caller.FactionId = null;
			caller.FactionRoleId = null;
			await RefreshFaction( factionId );
		} );
	}

	[Rpc.Host]
	public void SetMemberRoleHost( Guid factionId, long targetSteamId, Guid roleId )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:faction:setrole", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );
		if ( caller == null )
		{
			return;
		}

		if ( !HasFactionPermission( caller, factionId, FactionPermission.SetRanks ) )
		{
			caller.Error( "No permission" );
			return;
		}

		var target = GameUtils.GetPlayerById( targetSteamId );

		_ = GameTask.RunInThreadAsync( async () =>
		{
			await ServerApiClient.AddFactionMember( factionId, new AddFactionMemberDto
			{
				PlayerId = targetSteamId, RoleId = roleId
			} );

			await GameTask.MainThread();

			if ( target != null && target.IsValid() )
			{
				target.FactionRoleId = roleId;
			}
		} );
	}

	[Rpc.Host]
	public void DeleteFactionRoleHost( Guid factionId, Guid roleId )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:faction:role:delete", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );
		if ( caller == null )
		{
			return;
		}

		if ( !HasFactionPermission( caller, factionId, FactionPermission.SetRanks ) )
		{
			caller.Error( "No permission" );
			return;
		}

		_ = GameTask.RunInThreadAsync( async () =>
		{
			await ServerApiClient.DeleteFactionRole( factionId, roleId );

			await GameTask.MainThread();

			FactionRoles.Remove( roleId );
		} );
	}
}

using Dxura.RP.Game.Tools;
using Dxura.RP.Game.UI;
using Dxura.RP.Shared;
using Sandbox.Audio;
using System.Text.Json;
using WorldPanel=Sandbox.WorldPanel;

namespace Dxura.RP.Game;

public static class Debug
{
	[ConCmd( "dx_toggle_env_probes" )]
	public static void ToggleEnvProbes()
	{
		var envProbes = Sandbox.Game.ActiveScene.Components.GetAll<EnvmapProbe>( FindMode.EverythingInSelfAndDescendants );
		foreach ( var probe in envProbes )
		{
			probe.Enabled = !probe.Enabled;
		}
	}

	[ConCmd( "dx_toggle_footsteps" )]
	public static void ToggleFootsteps()
	{
		var playerControllers = Sandbox.Game.ActiveScene.Components.GetAll<PlayerController>( FindMode.EverythingInSelfAndDescendants );
		foreach ( var playerController in playerControllers )
		{
			playerController.EnableFootstepSounds = !playerController.EnableFootstepSounds;
		}
	}

	[ConCmd( "dx_constructs_freeze" )]
	public static void FreezeConstructs()
	{
		if ( !AdminSystem.Instance.IsValid() )
		{
			return;
		}

		AdminSystem.Instance.FreezeConstructs();
	}

	[ConCmd( "dx_list_world_panels" )]
	public static void ListWorldPanels()
	{
		var worldPanels = Sandbox.Game.ActiveScene.GetAllComponents<WorldPanel>();
		foreach ( var worldPanel in worldPanels )
		{
			Log.Info( $"World Panel: {worldPanel.GameObject.Root.Name}" );
		}
	}

	[ConCmd( "dx_list_renderers" )]
	public static void ListRenderers()
	{
		var skinnedModelRenderers = Sandbox.Game.ActiveScene.GetAllComponents<SkinnedModelRenderer>();
		foreach ( var skinnedModelRenderer in skinnedModelRenderers )
		{
			Log.Info( $"Skinned Model Renderer: {skinnedModelRenderer.GameObject.Root.Name}, specifically {skinnedModelRenderer.GameObject.Name}" );
		}
	}

	[ConCmd( "dx_toggle_soundscape" )]
	public static void ToggleSoundScape()
	{
		var soundscapes = Sandbox.Game.ActiveScene.Components.GetAll<SoundscapeTrigger>( FindMode.EverythingInSelfAndDescendants );
		foreach ( var scape in soundscapes )
		{
			scape.Enabled = !scape.Enabled;
		}
	}

	[ConCmd( "dx_verify_server_optimize" )]
	public static void VerifyServerOptimize()
	{
		foreach ( var player in GameUtils.Players )
		{
			var playerController = player.Components.Get<PlayerController>( FindMode.EverythingInSelfAndDescendants );
			var playerRenderer = player.Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants );
			var playerHitboxes = player.Components.Get<ModelHitboxes>( FindMode.EverythingInSelfAndDescendants );

			Log.Info( $"Player: {player.DisplayName}, Controller: {(playerController.Enabled ? "Enabled" : "Disabled")}, Renderer: {(playerRenderer.Enabled ? "Enabled" : "Disabled")}, Hitboxes: {(playerHitboxes.Enabled ? "Enabled" : "Disabled")}" );
		}
	}

	[ConCmd( "dx_sound_handles" )]
	public static void ListSoundHandles()
	{
		void ListVoices( Mixer node, int depth = 0 )
		{
			var indent = new string( ' ', depth * 2 );
			var voiceCount = node.Meter.Current.VoiceCount;
			Log.Info( $"{indent}- {node.Name}: VoiceCount = {voiceCount}" );

			foreach ( var child in node.GetChildren() )
			{
				ListVoices( child, depth + 1 );
			}
		}

		ListVoices( Mixer.Default );
	}


	[ConCmd( "dx_player_status" )]
	public static void GetPlayerStatus()
	{
		var localPlayer = Player.Local;

		if ( !localPlayer.IsValid() )
		{
			return;
		}

		Log.Info( $"Player Status for {localPlayer.DisplayName}:" );
		foreach ( var (status, info) in localPlayer.Statuses )
		{
			Log.Info( $"- {status} , Duration: {(info.Expiry.HasValue ? $"{info.Expiry.Value}s" : "Permanent")})" );
		}
	}

	[ConCmd( "dx_events_list" )]
	public static void ListEvents()
	{
		var eventTypes = TypeLibrary.GetTypes<BaseEvent>();
		foreach ( var eventType in eventTypes )
		{
			if ( eventType.IsAbstract || eventType.IsGenericType )
			{
				continue;
			}

			var instance = TypeLibrary.Create<BaseEvent>( eventType.TargetType );
			Log.Info( $"{instance.Identifier}: {instance.Name} - {instance.Description} ({instance.Duration}s)" );
		}
	}

	[ConCmd( "dx_events_toggle" )]
	public static void ToggleEvent( string eventIdentifier )
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		AdminSystem.Instance.ToggleEventHost( eventIdentifier );
	}

	[ConCmd( "dx_set_renderers" )]
	public static void SetRenderers( bool enabled )
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugAccess ) )
		{
			return;
		}

		var renderers = Sandbox.Game.ActiveScene.Components.GetAll<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		foreach ( var renderer in renderers )
		{
			renderer.Enabled = enabled;
		}
	}

	[ConCmd( "dx_connections" )]
	public static void ListConnections()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugAccess ) )
		{
			return;
		}

		AdminSystem.Instance.RequestConnectionStatsHost();
	}

	[ConCmd( "dx_mute" )]
	public static void ToggleMixer()
	{
		Mixer.Master.Mute = !Mixer.Master.Mute;
	}

	[ConCmd( "dx_snapshot_save" )]
	public static void SaveStateManual()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		SnapshotSystem.Current.SnapshotManualHost();
	}

	[ConCmd( "dx_occlude_doors" )]
	public static void OccludeDoors()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		var doors = Sandbox.Game.ActiveScene.Components.GetAll<Door>( FindMode.EverythingInSelfAndDescendants );
		foreach ( var door in doors )
		{
			door.Tags.Toggle( Constants.OccludableTag );
		}
	}

	[ConCmd( "dx_occlude_glass" )] public static void OccludeGlass()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		var glasses = Sandbox.Game.ActiveScene.Components.GetAll<Glass>( FindMode.EverythingInSelfAndDescendants );
		foreach ( var glass in glasses )
		{
			glass.Tags.Toggle( Constants.OccludableTag );
		}
	}

	[ConCmd( "dx_toggle_map" )] public static void ToggleMap()
	{
		var map = Sandbox.Game.ActiveScene.Components.GetAll<MapInstance>( FindMode.EverythingInSelfAndDescendants ).FirstOrDefault();

		if ( !map.IsValid() )
		{
			return;
		}

		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		map.Enabled = !map.Enabled;
	}

	[ConCmd( "dx_spawn_npc" )] public static void SpawnNpc()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		AdminSystem.Instance.SpawnNpcHost();
	}

	[ConCmd( "dx_entities" )] public static void LogEntities()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		var components = Sandbox.Game.ActiveScene.Components.GetAll<BaseEntity>( FindMode.EverythingInSelfAndDescendants ).ToList();
		Log.Info( $"Total Entities: {components.Count}" );

		var componentTypes = components.GroupBy( c => c.GetType().Name )
			.Select( g => new
			{
				Type = g.Key, Count = g.Count()
			} )
			.OrderByDescending( x => x.Count );

		foreach ( var type in componentTypes )
		{
			Log.Info( $"  {type.Type}: {type.Count}" );
		}
	}
	[ConCmd( "dx_networked" )] public static void LogNetworked()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		var count = Sandbox.Game.ActiveScene.Children.Count( x => x.NetworkMode == NetworkMode.Object );
		Log.Info( $"Total Networked Objects: {count}" );
	}

	[ConCmd( "dx_constructs" )] public static void LogConstructs()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		var components = Sandbox.Game.ActiveScene.Components.GetAll<IConstruct>( FindMode.EverythingInSelfAndDescendants ).ToList();
		Log.Info( $"Total Constructs: {components.Count}" );

		var componentTypes = components.GroupBy( c => c.GetType().Name )
			.Select( g => new
			{
				Type = g.Key, Count = g.Count()
			} )
			.OrderByDescending( x => x.Count );

		foreach ( var type in componentTypes )
		{
			Log.Info( $"  {type.Type}: {type.Count}" );
		}
	}

	[ConCmd( "dx_wire_tick" )] public static void SetWireTick( float tick )
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugAccess ) )
		{
			return;
		}

		switch ( tick )
		{
			case 0f:
				// 0 = disable wire processing completely
				AdminSystem.Instance.SetWireTick( float.MaxValue );
				Log.Info( "Wire processing disabled" );
				break;
			case < 0f:
				// Negative tick run every fixed update (no delay)
				AdminSystem.Instance.SetWireTick( 0f );
				Log.Info( "Wire processing set to every fixed update" );
				break;
			case > 0f:
				// Positive value = set tick interval
				AdminSystem.Instance.SetWireTick( tick );
				Log.Info( $"Wire tick set to {tick}s" );
				break;
		}
	}

	[ConCmd( "dx_wire_values" )] public static void LogWireValues()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		var components = Sandbox.Game.ActiveScene.Components.GetAll<Wire.IWireComponent>( FindMode.EverythingInSelfAndDescendants );
		foreach ( var component in components )
		{
			Log.Info( $"Component: {component.GetType().Name} ({component.GameObject.Name})" );

			foreach ( var output in component.GetOutputPorts() )
			{
				var value = Wire.Wire.Current.GetOutputValue( component, output.Id );
				Log.Info( $"  Output '{output.Id}' ({output.Type}): {value}" );
			}

			foreach ( var input in component.GetInputPorts() )
			{
				var value = Wire.Wire.Current.GetInputValue( component, input.Id );
				Log.Info( $"  Input '{input.Id}' ({input.Type}): {value}" );
			}
		}
	}

	[ConCmd( "dx_wire_state" )] public static void LogWireState()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		var connections = Wire.Wire.Current.GetConnections().ToList();
		var components = Sandbox.Game.ActiveScene.Components.GetAll<Wire.IWireComponent>( FindMode.EverythingInSelfAndDescendants ).ToList();

		Log.Info( "=== Wire System Statistics ===" );
		Log.Info( $"Total Components: {components.Count}" );
		Log.Info( $"Total Connections: {connections.Count}" );
		Log.Info( $"Wire Tick Override: {Wire.Wire.Current.WireTickOverride?.ToString() ?? "None"}" );

		// Connection statistics
		var connectionsByType = connections.GroupBy( c => c.Source.GetType().Name )
			.Select( g => new
			{
				Type = g.Key, Count = g.Count()
			} )
			.OrderByDescending( x => x.Count );

		Log.Info( "Connections by source type:" );
		foreach ( var type in connectionsByType )
		{
			Log.Info( $"  {type.Type}: {type.Count}" );
		}

		// Find components with most connections
		var componentConnections = components.Select( c => new
		{
			Component = c, Count = Wire.Wire.Current.GetConnections( c ).Count()
		} ).OrderByDescending( x => x.Count ).Take( 5 );

		Log.Info( "Components with most connections:" );
		foreach ( var item in componentConnections )
		{
			if ( item.Count > 0 )
			{
				Log.Info( $"  {item.Component.GetType().Name} ({item.Component.GameObject.Name}): {item.Count}" );
			}
		}
	}

	[ConCmd( "dx_bank_all" )] public static void BankAll()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		AdminSystem.Instance.BankAllHost();
	}

	[ConCmd( "dx_game_dupe" )]
	public static void DumpGameDupe()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		var dupe = DuplicatorTool.Duplicate( Sandbox.Game.ActiveScene.Children, Vector3.Zero, true );
		if ( dupe == null )
		{
			return;
		}

		DupeFileManager.SaveDupe( dupe );
	}

	[ConCmd( "dx_restart" )]
	public static void Restart( string reason, bool saveState = true, float delaySeconds = 120 )
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugFull ) )
		{
			return;
		}

		AdminSystem.Instance.RestartHost( reason, saveState, delaySeconds );
	}

	[ConCmd( "dx_nuke" )] public static void Nuke()
	{
		AdminSystem.Instance.NukeHost();
	}

	[ConCmd( "dx_rank_info" )]
	public static void RankInfo()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugAccess ) )
		{
			return;
		}

		var rank = RankSystem.Instance.GetPlayerRank( Sandbox.Game.SteamId );
		if ( rank == null )
		{
			Log.Info( "No rank assigned." );
			return;
		}

		Log.Info( $"Rank: {rank.Name} (Order: {rank.Order}, Flags: {rank.Flags})" );
		Log.Info( $"Permissions ({rank.Permissions.Count}):" );
		foreach ( var perm in rank.Permissions )
		{
			Log.Info( $"  {perm}" );
		}
	}

	[ConCmd( "dx_status_list" )]
	public static void ListStatus()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugAccess ) )
		{
			return;
		}

		var ids = Status.Current.GetAllStatuses().Select( status => status.Id );
		Log.Info( string.Join( ", ", ids ) );
	}

	[ConCmd( "dx_status_add" )]
	public static void AddStatus( string playerName, string statusId, float? duration = null )
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugAccess ) )
		{
			return;
		}

		AdminSystem.Instance.AddStatusHost( playerName, statusId, duration );
	}

	[ConCmd( "dx_status_remove" )]
	public static void RemoveStatus( string playerName, string statusId )
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugAccess ) )
		{
			return;
		}

		AdminSystem.Instance.RemoveStatusHost( playerName, statusId );
	}
}

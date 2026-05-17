using Sandbox.Speech;

namespace Dxura.RP.Game;

public partial class Npc
{
	protected virtual void OnStartEffects() {}
	protected virtual void OnUpdateEffects() {}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	protected virtual void OnDamageEffects( DamageInfo damageInfo )
	{
		var phrases = new List<string>
		{
			"Ow! That wasn't very cash money of you!",
			"I can't believe you've done this!",
			"My disappointment is immeasurable and my day is ruined!",
			"Bruh moment detected!",
			"That's gonna leave a mark!",
			"I need a medic!",
			"That tickled... in a bad way!",
			"Is that all you've got?",
			"I've had worse paper cuts!",
			"This is fine. Everything is fine.",
			"That's not very nice!",
			"I should have stayed in bed today.",
			"Why are we still here? Just to suffer?",
			"That's a yikes from me!",
			"Time to reconsider my life choices.",
			"Did someone get the license plate?",
			"This is less than ideal.",
			"Not my finest moment.",
			"Could we talk about this?",
			"I'm getting too old for this!",
			"That's gonna cost ya!",
			"Was it something I said?",
			"This is just a flesh wound!",
			"I need an adult!",
			"Who taught you manners?",
			"That's not what friends do!",
			"My lawyer will hear about this!",
			"I'm calling the manager!",
			"This isn't what I signed up for!",
			"Do you have a permit for that?",
			"That's coming out of your paycheck!",
			"Not cool, bro!",
			"This is so sad.",
			"I'm telling mom!",
			"That's gonna need some ice.",
			"Well, that escalated quickly!",
			"Plot twist: that hurt!",
			"Oof size: large!",
			"That's a lot of damage!",
			"I should have learned to dodge!",
			"Did we just become frenemies?",
			"This is sub-optimal.",
			"Task failed successfully!",
			"Error 404: Defense not found!",
			"That's one way to say hello!",
			"I'm having a bad day.",
			"Who needs enemies with friends like these?",
			"That's not very poggers!",
			"Emotional damage!",
			"My disappointment is astronomical!",
			"That wasn't part of the plan!",
			"I need a vacation.",
			"Is this covered by insurance?",
			"That's gonna leave a bruise!",
			"I thought we were cool!",
			"This is not the way!",
			"Can we start over?",
			"That's a no from me, dawg!",
			"Why are you running?",
			"This ain't it, chief!",
			"That's rough, buddy!",
			"Time for a tactical retreat!",
			"I should have read the manual!",
			"Did I forget to pay my karma bill?",
			"That's not what the prophecy foretold!",
			"I'm too sober for this!",
			"Who's your anger management coach?",
			"This is beyond science!",
			"That's not very wholesome!",
			"I need new friends!",
			"That wasn't very professional!",
			"Is this a social experiment?",
			"Can we talk about our feelings instead?",
			"That's gonna cost extra!",
			"I didn't sign a waiver for this!",
			"This is not what I expected!",
			"Time to update my resume!",
			"That's not in my job description!",
			"I should have stayed in training!",
			"This is not the content I subscribed for!",
			"That's gonna need some explaining!",
			"I'm sensing some hostility here!",
			"This is not the optimal outcome!",
			"That's not very team player of you!",
			"I need a raise for this!",
			"Who's covering my medical bills?",
			"That's not in the employee handbook!",
			"I should have taken that other job!",
			"This is not the collaboration I wanted!",
			"That's not how you make friends!",
			"I need better insurance!",
			"Time to file a complaint!",
			"That's not following protocol!",
			"I should have read the fine print!",
			"This is not the networking I had in mind!",
			"That's a strange way to introduce yourself!",
			"I need a second opinion!",
			"Time to call HR!",
			"That's not standard procedure!",
			"I should have worn protection!",
			"This is not the experience I wanted!",
			"That's not very professional development!",
			"I need hazard pay!",
			"Time to update my will!",
			"That's not what the training covered!"
		};

		var synth = new Synthesizer();
		synth.WithText( Random.Shared.FromList( phrases! ) );
		var sound = synth.Play();

		if ( sound.IsValid() )
		{
			sound.Position = WorldPosition;
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	protected virtual void OnKillEffects()
	{
		BodyPhysics.Enabled = true;
	}


}

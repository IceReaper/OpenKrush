using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Kknd.Traits.Technicians;

namespace OpenRA.Mods.Kknd.Activities
{
	public class TechnicianRepair : Enter
	{
		private readonly Actor target;
		private readonly int amount;
		private readonly int duration;
		private readonly string voiceEnter;

		public TechnicianRepair(Actor self, Actor target, int amount, int duration, string voiceEnter) : base(self, target, EnterBehaviour.Dispose)
		{
			this.target = target;
			this.amount = amount;
			this.duration = duration;
			this.voiceEnter = voiceEnter;
		}

		protected override void OnInside(Actor self)
		{
			target.Trait<TechnicianRepairable>().Add(amount, duration);

			if (self.Owner == self.World.LocalPlayer)
				self.PlayVoice(voiceEnter);
		}
	}
}

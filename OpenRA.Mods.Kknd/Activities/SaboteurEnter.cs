using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Kknd.Traits.Saboteurs;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Activities
{
	public class SaboteurEnter : Enter
	{
		private readonly Actor target;
		private readonly SaboteurInfo saboteurInfo;

		public SaboteurEnter(Actor self, Actor target) : base(self, target, EnterBehaviour.Dispose)
		{
			this.target = target;
			saboteurInfo = self.Info.TraitInfo<SaboteurInfo>();
		}

		protected override void OnInside(Actor self)
		{
			var conquerable = target.Trait<SaboteurConquerable>();

			if (self.Owner == self.World.LocalPlayer)
			{
				if (target.Owner.Stances[self.Owner].HasStance(Stance.Ally))
					self.PlayVoice(saboteurInfo.VoiceEnterAlly);
				else if (conquerable.Population == 0)
					self.PlayVoice(saboteurInfo.VoiceConquered);
				else
					self.PlayVoice(saboteurInfo.VoiceEnterEnemy);
			}

			conquerable.Enter(target, self);
		}
	}
}

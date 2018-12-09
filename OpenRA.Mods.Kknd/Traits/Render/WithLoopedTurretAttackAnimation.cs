using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Render
{
	[Desc("Plays a looped attack animation regardless whether the armament shoots or waits to shoot.")]
	public class WithLoopedTurretAttackAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteTurretInfo>, Requires<ArmamentInfo>
	{
		[Desc("Armament name")]
		public readonly string Armament = "primary";

		[Desc("Turret name")]
		public readonly string Turret = "primary";

		[Desc("Displayed while attacking.")]
		[SequenceReference] public readonly string Sequence = null;

		public override object Create(ActorInitializer init) { return new WithLoopedTurretAttackAnimation(init, this); }
	}

	public class WithLoopedTurretAttackAnimation : ConditionalTrait<WithLoopedTurretAttackAnimationInfo>, ITick, INotifyAttack
	{
		readonly WithSpriteTurret wst;
		private int attacking;

		public WithLoopedTurretAttackAnimation(ActorInitializer init, WithLoopedTurretAttackAnimationInfo info) : base(info)
		{
			wst = init.Self.TraitsImplementing<WithSpriteTurret>().Single(st => st.Info.Turret == info.Turret);
		}

		void ITick.Tick(Actor self)
		{
			if (attacking > 0)
			{
				attacking--;
				if (wst.DefaultAnimation.CurrentSequence.Name != Info.Sequence)
					wst.PlayCustomAnimation(self, Info.Sequence);
			}
			else if (wst.DefaultAnimation.CurrentSequence.Name == Info.Sequence)
				wst.CancelCustomAnimation(self);
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			attacking = 1;
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
		{
		}
	}
}

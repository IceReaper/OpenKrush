using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Kknd.Traits.Production
{
	[Desc("This special airstrike version makes bombers fly a base-to-target route.")]
	public class AdvancedAirstrikePowerInfo : AirstrikePowerInfo
	{
		public override object Create(ActorInitializer init) { return new AdvancedAirstrikePower(init.Self, this); }
	}

	public class AdvancedAirstrikePower : AirstrikePower
	{
		public AdvancedAirstrikePower(Actor self, AdvancedAirstrikePowerInfo info) : base(self, info) { }

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			var facing = (self.World.Map.CenterOfCell(order.TargetLocation) - self.CenterPosition).Yaw.Facing;
			SendAirstrike(self, self.World.Map.CenterOfCell(order.TargetLocation), false, facing);
		}
	}
}

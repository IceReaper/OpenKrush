using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Orders
{
	class DeployOnActorOrderTargeter : UnitOrderTargeter
	{
		private string[] validTargets;

		public DeployOnActorOrderTargeter(string[] validTargets, string cursor) : base("Move", 6, cursor, false, true)
		{
			this.validTargets = validTargets;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			return validTargets.Contains(target.Info.Name);
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}
	}
}

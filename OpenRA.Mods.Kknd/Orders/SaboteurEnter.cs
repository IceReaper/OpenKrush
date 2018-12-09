using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Kknd.Traits.Saboteurs;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Orders
{
	class SaboteurEnterOrderTargeter : UnitOrderTargeter
	{
		public const string Id = "SaboteurEnter";

		private string cursor;
		private string blockedCursor;

		// TODO test if this "7" makes saboteurs saboteur cursor more important than attacking.
		public SaboteurEnterOrderTargeter(string cursor, string blockedCursor) : base(Id, 7, cursor, false, true)
		{
			this.cursor = cursor;
			this.blockedCursor = blockedCursor;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			cursor = null;

			var saboteurConquerable = target.TraitOrDefault<SaboteurConquerable>();
			var saboteurConquerableInfo = target.Info.TraitInfoOrDefault<SaboteurConquerableInfo>();

			if (saboteurConquerable == null || saboteurConquerable.IsTraitDisabled)
				return false;

			if (self.Owner.Stances[target.Owner] == Stance.Ally && saboteurConquerable.Population == saboteurConquerableInfo.MaxPopulation)
			{
				cursor = blockedCursor;
				return false;
			}

			cursor = this.cursor;
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}
	}
}

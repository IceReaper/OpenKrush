using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Kknd.Traits.Altar;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Orders
{
	class SacrificeOrderTargeter : UnitOrderTargeter
	{
		public const string Id = "Sacrifice";

		private string cursor;

		public SacrificeOrderTargeter(string cursor) : base(Id, 6, cursor, false, true)
		{
			this.cursor = cursor;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			cursor = null;

			if (!target.Info.HasTraitInfo<AltarInfo>())
				return false;

			if (target.Trait<Altar>().IsTraitDisabled)
				return false;

			if (self.Owner != target.Owner)
				return false;

			cursor = this.cursor;
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}
	}
}

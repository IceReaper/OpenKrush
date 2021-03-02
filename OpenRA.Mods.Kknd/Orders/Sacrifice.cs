#region Copyright & License Information
/*
 * Copyright 2007-2021 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Kknd.Traits.Altar;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Orders
{
	class SacrificeOrderTargeter : UnitOrderTargeter
	{
		public const string Id = "Sacrifice";

		private string cursor;

		public SacrificeOrderTargeter(string cursor)
			: base(Id, 6, cursor, false, true)
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

#region Copyright & License Information

/*
 * Copyright 2007-2021 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Mechanics.Sacrificing.Orders
{
	using Common.Orders;
	using OpenRA.Traits;

	public class SacrificeOrderTargeter : UnitOrderTargeter
	{
		public const string Id = "Sacrifice";

		private readonly string cursor;

		public SacrificeOrderTargeter(string cursor)
			: base(SacrificeOrderTargeter.Id, 6, cursor, false, true)
		{
			this.cursor = cursor;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if (!SacrificingUtils.CanEnter(self, target))
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

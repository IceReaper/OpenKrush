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

using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenKrush.Mechanics.Technicians.Orders
{
	public class TechnicianEnterOrderTargeter : UnitOrderTargeter
	{
		public const string Id = "TechnicianEnter";

		private readonly string cursorAllowed;
		private readonly string cursorForbidden;

		public TechnicianEnterOrderTargeter(string cursorAllowed, string cursorForbidden)
			: base(Id, 6, null, false, true)
		{
			this.cursorAllowed = cursorAllowed;
			this.cursorForbidden = cursorForbidden;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if (!TechnicianUtils.CanEnter(self, target))
			{
				cursor = cursorForbidden;

				return false;
			}

			cursor = cursorAllowed;

			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}
	}
}

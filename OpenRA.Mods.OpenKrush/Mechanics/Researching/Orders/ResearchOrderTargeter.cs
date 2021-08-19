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

namespace OpenRA.Mods.OpenKrush.Mechanics.Researching.Orders
{
	using Common.Orders;
	using OpenRA.Traits;
	using System;

	public class ResearchOrderTargeter : UnitOrderTargeter
	{
		public const string Id = "Research";

		private readonly string cursorAllowed;
		private readonly string cursorForbidden;

		public ResearchOrderTargeter(string cursorAllowed, string cursorForbidden)
			: base(ResearchOrderTargeter.Id, 6, cursorAllowed, false, true)
		{
			this.cursorAllowed = cursorAllowed;
			this.cursorForbidden = cursorForbidden;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string? cursor)
		{
			var action = ResearchUtils.GetAction(self, target);

			switch (action)
			{
				case ResearchAction.Start:
					cursor = this.cursorAllowed;

					return true;

				case ResearchAction.Stop:
					cursor = this.cursorForbidden;

					return true;

				case ResearchAction.None:
					cursor = null;

					return false;

				default:
					throw new ArgumentOutOfRangeException(Enum.GetName(action));
			}
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}
	}
}

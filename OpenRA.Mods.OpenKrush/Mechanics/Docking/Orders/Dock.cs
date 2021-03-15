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

namespace OpenRA.Mods.OpenKrush.Mechanics.Docking.Orders
{
	using Common.Orders;
	using OpenRA.Traits;
	using Traits;

	public class DockOrderTargeter : UnitOrderTargeter
	{
		public const string Id = "Dock";

		public DockOrderTargeter()
			: base(DockOrderTargeter.Id, 6, null, false, true)
		{
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

			foreach (var dock in target.TraitsImplementing<Dock>())
			{
				if (dock.IsTraitDisabled)
					continue;

				var dockAction = dock.GetDockAction(self);

				if (dockAction == null)
					continue;

				cursor = dockAction.Info.Cursor;

				return true;
			}

			return false;
		}
	}
}

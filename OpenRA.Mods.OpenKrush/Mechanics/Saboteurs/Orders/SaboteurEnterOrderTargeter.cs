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

namespace OpenRA.Mods.OpenKrush.Mechanics.Saboteurs.Orders;

using Common.Orders;
using OpenRA.Traits;

public class SaboteurEnterOrderTargeter : UnitOrderTargeter
{
	public const string Id = "SaboteurEnter";

	private readonly string cursorAllowed;
	private readonly string cursorForbidden;

	public SaboteurEnterOrderTargeter(string cursorAllowed, string cursorForbidden)
		: base(SaboteurEnterOrderTargeter.Id, 7, null, false, true)
	{
		this.cursorAllowed = cursorAllowed;
		this.cursorForbidden = cursorForbidden;
	}

	public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
	{
		var result = SaboteurUtils.CanEnter(self, target, out var blocked);
		cursor = blocked ? this.cursorForbidden : this.cursorAllowed;

		return result || blocked;
	}

	public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
	{
		return false;
	}
}

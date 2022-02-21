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

namespace OpenRA.Mods.OpenKrush.Mechanics.Oil.Activities;

using Common.Orders;
using OpenRA.Traits;

public class DeployOnActorOrderTargeter : UnitOrderTargeter
{
	private readonly string[] validTargets;

	public DeployOnActorOrderTargeter(string[] validTargets, string cursor)
		: base("Move", 6, cursor, false, true)
	{
		this.validTargets = validTargets;
	}

	public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
	{
		return this.validTargets.Contains(target.Info.Name);
	}

	public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
	{
		return this.validTargets.Contains(target.Info.Name);
	}
}

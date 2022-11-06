#region Copyright & License Information

/*
 * Copyright 2007-2022 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Mechanics.Docking.Traits;

using Common.Traits;
using JetBrains.Annotations;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public abstract class DockActionInfo : ConditionalTraitInfo
{
	[Desc("Cursor to use when docking is possible.")]
	public readonly string Cursor = "dock";

	[Desc("Name of the dock this action is assigned to..")]
	public readonly string Name = "Dock";
}

public abstract class DockAction : ConditionalTrait<DockActionInfo>
{
	protected DockAction(DockActionInfo info)
		: base(info)
	{
	}

	public abstract bool CanDock(Actor self, Actor actor);

	public virtual void OnDock(Actor self)
	{
	}

	public abstract bool Process(Actor self, Actor actor);

	public virtual void OnUndock()
	{
	}
}

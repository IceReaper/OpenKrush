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

using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Kknd.Traits.Docking
{
	public abstract class DockActionInfo : ConditionalTraitInfo
	{
		[Desc("Cursor to use when docking is possible.")]
		public readonly string Cursor = "dock";
	}

	public abstract class DockAction : ConditionalTrait<DockActionInfo>
	{
		public DockAction(DockActionInfo info)
			: base(info) { }

		public abstract bool CanDock(Actor actor, Dockable dockable);

		public virtual void OnDock() { }
		public abstract bool Process(Actor actor);
		public virtual void OnUndock() { }
	}
}

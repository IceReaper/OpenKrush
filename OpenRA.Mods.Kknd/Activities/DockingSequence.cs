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

using OpenRA.Activities;
using OpenRA.Mods.Kknd.Traits.Docking;

namespace OpenRA.Mods.Kknd.Activities
{
	public abstract class DockingSequence : Activity
	{
		public abstract void Dock(Actor dockableActor, Dockable dockable, Actor dockActor, Dock dock);
		public abstract void Undock(Actor dockableActor, Dockable dockable, Actor dockActor, Dock dock);
	}
}

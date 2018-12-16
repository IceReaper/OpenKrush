#region Copyright & License Information
/*
 * Copyright 2016-2018 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits.Docking;

namespace OpenRA.Mods.Kknd.Traits.Repairbay
{
	public class RepairableVehicleInfo : DockableInfo
	{
		public override object Create(ActorInitializer init) { return new RepairableVehicle(this); }
	}

	public class RepairableVehicle : Dockable {
		public RepairableVehicle(RepairableVehicleInfo info) : base(info) { }
	}
}

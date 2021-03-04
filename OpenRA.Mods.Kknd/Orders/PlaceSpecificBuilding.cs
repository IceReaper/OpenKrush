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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Kknd.Orders
{
	public class PlaceSpecificBuildingOrderGenerator : PlaceBuildingOrderGenerator
	{
		public readonly string Name;

		public PlaceSpecificBuildingOrderGenerator(ProductionQueue queue, string name, WorldRenderer worldRenderer)
			: base(queue, name, worldRenderer)
		{
			Name = name;
		}
	}
}

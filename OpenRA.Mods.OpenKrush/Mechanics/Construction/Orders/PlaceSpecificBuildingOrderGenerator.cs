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

namespace OpenRA.Mods.OpenKrush.Mechanics.Construction.Orders
{
	using Common.Orders;
	using Common.Traits;
	using Graphics;

	public class PlaceSpecificBuildingOrderGenerator : PlaceBuildingOrderGenerator
	{
		public readonly string Name;

		public PlaceSpecificBuildingOrderGenerator(ProductionQueue queue, string name, WorldRenderer worldRenderer)
			: base(queue, name, worldRenderer)
		{
			this.Name = name;
		}

		protected override IEnumerable<Order> InnerOrder(World world, CPos cell, MouseInput mi)
		{
			return this.queue.Actor.Owner.PlayerActor.Trait<PlayerResources>() is { Cash: 0 } ? Array.Empty<Order>() : base.InnerOrder(world, cell, mi);
		}
	}
}

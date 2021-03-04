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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Kknd.Mechanics.Construction.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Construction.Orders
{
	public class SellOrderGenerator : OrderGenerator
	{
		public const string Id = "Sell";

		private IEnumerable<TraitPair<DeconstructSellable>> sellableActors;

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
				world.CancelInputMode();
			else
			{
				var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a => sellableActors.Any(e => e.Actor == a));
				if (actor != null)
					yield return new Order(Id, actor, false);
			}
		}

		protected override void Tick(World world)
		{
			sellableActors = world.ActorsWithTrait<DeconstructSellable>().Where(e => e.Actor.Owner == world.LocalPlayer && !e.Trait.IsTraitDisabled);

			if (!sellableActors.Any())
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (sellableActors == null)
				return null;

			var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a => sellableActors.Any(e => e.Actor == a));

			return actor != null ? "sell" : null;
		}
	}
}

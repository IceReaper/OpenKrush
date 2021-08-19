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
	using Graphics;
	using System.Collections.Generic;
	using System.Linq;
	using Traits;

	public class SellOrderGenerator : OrderGenerator
	{
		public const string Id = "Sell";

		private IEnumerable<TraitPair<DeconstructSellable>>? sellableActors;

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
				world.CancelInputMode();
			else
			{
				var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a => this.sellableActors?.Any(e => e.Actor.Equals(a)) ?? false);

				if (actor != null)
					yield return new(SellOrderGenerator.Id, actor, false);
			}
		}

		protected override void Tick(World world)
		{
			this.sellableActors = world.ActorsWithTrait<DeconstructSellable>().Where(e => e.Actor.Owner == world.LocalPlayer && !e.Trait.IsTraitDisabled);

			if (!this.sellableActors.Any())
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
		{
			yield break;
		}

		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
		{
			yield break;
		}

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			yield break;
		}

		protected override string? GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (this.sellableActors == null)
				return null;

			var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a => this.sellableActors.Any(e => e.Actor.Equals(a)));

			return actor != null ? "sell" : null;
		}
	}
}

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

namespace OpenRA.Mods.OpenKrush.Mechanics.Researching.Orders
{
	using System.Collections.Generic;
	using System.Linq;
	using OpenRA.Orders;
	using OpenRA.Traits;
	using Traits;

	public class ResearchOrderGenerator : UnitOrderGenerator
	{
		private IEnumerable<Actor> researchActors = new List<Actor>();

		public override void Tick(World world)
		{
			researchActors = world.ActorsHavingTrait<Researches>().Where(e => e.Owner == world.LocalPlayer);

			if (!researchActors.Any())
				world.CancelInputMode();
		}

		public override IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
				world.CancelInputMode();
			else
			{
				foreach (var actor in world.ActorMap.GetActorsAt(cell))
				foreach (var researchActor in researchActors)
				{
					var action = ResearchUtils.GetAction(researchActor, actor);

					if (action == ResearchAction.None)
						continue;

					yield return new Order(ResearchOrderTargeter.Id, researchActor, Target.FromActor(actor), true);
				}
			}
		}

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			foreach (var actor in world.ActorMap.GetActorsAt(cell))
			foreach (var researchActor in researchActors)
			{
				var action = ResearchUtils.GetAction(researchActor, actor);

				if (action == ResearchAction.None)
					continue;

				var info = researchActor.Info.TraitInfo<ResearchesInfo>();

				if (action == ResearchAction.Start)
					return info.Cursor;

				if (action == ResearchAction.Stop)
					return info.BlockedCursor;
			}

			return null;
		}

		public override bool InputOverridesSelection(World world, int2 xy, MouseInput mi)
		{
			return true;
		}
	}
}

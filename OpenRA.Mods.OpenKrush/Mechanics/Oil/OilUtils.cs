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

using System;
using System.Linq;
using OpenRA.Mods.OpenKrush.Mechanics.Docking.Traits;
using OpenRA.Mods.OpenKrush.Mechanics.Docking.Traits.Actions;
using OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits;

namespace OpenRA.Mods.OpenKrush.Mechanics.Oil
{
    public static class OilUtils
    {
		public static Actor GetMostUnderutilizedDrillrig(Player owner)
		{
			// We will ignore any oil sneaking actors.
			var tankers = owner.World.ActorsWithTrait<Tanker>()
				.Where(pair => pair.Actor.Owner == owner && pair.Trait.PreferedDrillrig != null)
				.Select(pair => pair.Trait)
				.ToArray();

			var powerstations = owner.World.ActorsWithTrait<PowerStation>()
				.Where(pair => pair.Actor.Owner == owner && !pair.Trait.IsTraitDisabled)
				.Select(pair => pair.Actor)
				.ToArray();

			return owner.World.ActorsWithTrait<Drillrig>()
				.Where(pair => pair.Actor.Owner == owner && IsUsable(pair.Actor, pair.Trait))
				.Select(pair => pair.Actor)
				.OrderBy(drillrig =>
				{
					// Assume a default distance of 1 when we have no powerstation nearby.
					var distance = 1;

					// We ignore the fact that Tankers could have assigned a different PowerStation.
					if (powerstations.Any())
						distance = Math.Max(distance, powerstations.Min(powerStation => (powerStation.CenterPosition - drillrig.CenterPosition).Length));

					// Using a large factor to avoid using a float.
					return 1024 * 1024 * tankers.Count(pair2 => pair2.PreferedDrillrig.Equals(drillrig)) / distance;
				})
				.FirstOrDefault();
		}

		public static Actor GetNearestPowerStation(Player owner, WPos origin)
		{
			return owner.World.ActorsWithTrait<PowerStation>()
				.Where(pair => pair.Actor.Owner == owner && IsUsable(pair.Actor, pair.Trait))
				.OrderBy(pair => (pair.Actor.CenterPosition - origin).Length)
				.Select(pair => pair.Actor)
				.FirstOrDefault();
		}

		public static bool IsUsable(Actor actor, Drillrig drillrig)
		{
			if (actor == null || actor.IsDead || !actor.IsInWorld)
				return false;

			var dock = actor.TraitOrDefault<Dock>();

			return dock != null && !dock.IsTraitDisabled && drillrig != null && !drillrig.IsTraitDisabled && drillrig.Current > 0;
		}

		public static bool IsUsable(Actor actor, PowerStation powerStation)
		{
			if (actor == null || actor.IsDead || !actor.IsInWorld)
				return false;

			var dock = actor.TraitOrDefault<Dock>();

			return dock != null && !dock.IsTraitDisabled && powerStation != null && !powerStation.IsTraitDisabled;
		}
    }
}

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

namespace OpenRA.Mods.OpenKrush.Traits.AI
{
	using System.Collections.Generic;
	using System.Linq;
	using Common.Traits;
	using Mechanics.Construction.Traits;
	using OpenRA.Traits;

	public class BotBaseBuilderInfo : TraitInfo
	{
		public override object Create(ActorInitializer init)
		{
			return new BotBaseBuilder(init.World);
		}
	}

	public class BotBaseBuilder : IBotTick, INotifyCreated
	{
		private readonly IEnumerable<string> bases;
		private ProductionQueue[] queues;

		public BotBaseBuilder(World world)
		{
			bases = world.Map.Rules.Actors.Where(a => a.Value.HasTraitInfo<BaseBuildingInfo>()).Select(e => e.Key);
		}

		void INotifyCreated.Created(Actor self)
		{
			queues = self.TraitsImplementing<ProductionQueue>().ToArray();
		}

		void IBotTick.BotTick(IBot bot)
		{
			HandleBuildings(bot);
		}

		private void HandleBuildings(IBot bot)
		{
			var queue = queues.FirstOrDefault(q => q.Info.Type == "building");

			if (queue == null)
				return;

			var buildings = bot.Player.World.Actors.Where(a => a.Owner == bot.Player && a.Info.HasTraitInfo<BuildingInfo>()).ToArray();

			var constructedBuildings = buildings.Where(
				building =>
				{
					var selfConstructing = building.TraitOrDefault<SelfConstructing>();
					return selfConstructing == null || !selfConstructing.IsConstructing;
				}).ToArray();

			if (!constructedBuildings.Any())
				return;

			var buildables = queue.BuildableItems().ToArray();

			// If we do not have a base, and we can place a base, we ALWAYS place it, regardless of anything else being build!
			if (!buildings.Any(building => bases.Contains(building.Info.Name)))
			{
				var baseActorInfo = buildables.FirstOrDefault(buildable => bases.Contains(buildable.Name));

				if (baseActorInfo != null)
				{
					PlaceConstruction(bot, constructedBuildings, baseActorInfo, PlacementType.InBase, queue);
					return;
				}
			}

			// If we are constructing anything, do not place anything to avoid delaying build times.
			if (constructedBuildings.Length < buildings.Length)
				return;

			// TODO continue to build base in order with priority!
			var randomBuilding = buildables.Where(b => !bases.Contains(b.Name)).RandomOrDefault(bot.Player.World.SharedRandom);

			if (randomBuilding != null)
				PlaceConstruction(bot, constructedBuildings, randomBuilding, PlacementType.InBase, queue);
		}

		private void PlaceConstruction(IBot bot, Actor[] buildings, ActorInfo actorInfo, PlacementType type, ProductionQueue queue)
		{
			var maxDistance = actorInfo.TraitInfoOrDefault<RequiresBuildableAreaInfo>()?.Adjacent ?? 0;
			var placeLocation = BotBaseBuilder.FindBuildLocation(bot.Player.World, ChooseBuildSource(buildings, type), maxDistance, actorInfo);

			if (placeLocation == null)
				return;

			bot.QueueOrder(new Order("PlaceBuilding", bot.Player.PlayerActor, Target.FromCell(bot.Player.World, placeLocation.Value), false)
			{
				TargetString = actorInfo.Name,
				ExtraData = queue.Actor.ActorID,
			});
		}

		private CPos ChooseBuildSource(Actor[] buildings, PlacementType type)
		{
			if (type == PlacementType.InBase)
				return (buildings.FirstOrDefault(b => bases.Contains(b.Info.Name)) ?? buildings.First()).Location;

			if (type == PlacementType.NearOil)
			{
				// TODO this should be used to place power stations.
			}

			if (type == PlacementType.TowardsEnemy)
			{
				// TODO this should be used to place defences.
			}

			if (type == PlacementType.AroundBase)
			{
				// TODO this should be used for wall placement.
			}

			return CPos.Zero;
		}

		private static CPos? FindBuildLocation(World world, CPos center, int maxRange, ActorInfo actorInfo)
		{
			var buildingInfo = actorInfo.TraitInfo<BuildingInfo>();

			return world.Map.FindTilesInAnnulus(center, 0, maxRange)
				.OrderBy(c => (c - center).LengthSquared)
				.FirstOrDefault(cell => world.CanPlaceBuilding(cell, actorInfo, buildingInfo, null));
		}
	}
}

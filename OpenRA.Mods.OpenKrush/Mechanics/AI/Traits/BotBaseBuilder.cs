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

namespace OpenRA.Mods.OpenKrush.Mechanics.AI.Traits
{
	using Common.Traits;
	using Construction.Traits;
	using JetBrains.Annotations;
	using Misc.Traits;
	using Oil.Traits;
	using OpenRA.Traits;
	using Production.Traits;
	using Repairbays.Traits;
	using Researching.Traits;

	[UsedImplicitly]
	public class BotBaseBuilderInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init)
		{
			return new BotBaseBuilder(init.World, this);
		}
	}

	public class BotBaseBuilder : ConditionalTrait<BotBaseBuilderInfo>, IBotTick
	{
		private readonly string[] bases;
		private readonly string[] powerStations;
		private readonly string[] researchers;
		private readonly string[] factories;
		private readonly string[] superWeapons;
		private readonly string[] moneyGenerators;
		private readonly string[] repairers;
		private ProductionQueue[] queues = Array.Empty<ProductionQueue>();
		private PlayerResources? resources;

		public BotBaseBuilder(World world, BotBaseBuilderInfo info)
			: base(info)
		{
			this.bases = world.Map.Rules.Actors.Where(a => a.Value.HasTraitInfo<BaseBuildingInfo>()).Select(e => e.Key).ToArray();
			this.powerStations = world.Map.Rules.Actors.Where(a => a.Value.HasTraitInfo<PowerStationInfo>()).Select(e => e.Key).ToArray();
			this.researchers = world.Map.Rules.Actors.Where(a => a.Value.HasTraitInfo<ResearchesInfo>()).Select(e => e.Key).ToArray();
			this.factories = world.Map.Rules.Actors.Where(a => a.Value.HasTraitInfo<AdvancedProductionInfo>()).Select(e => e.Key).ToArray();
			this.superWeapons = world.Map.Rules.Actors.Where(a => a.Value.HasTraitInfo<AdvancedAirstrikePowerInfo>()).Select(e => e.Key).ToArray();
			this.moneyGenerators = world.Map.Rules.Actors.Where(a => a.Value.HasTraitInfo<CashTricklerInfo>()).Select(e => e.Key).ToArray();
			this.repairers = world.Map.Rules.Actors.Where(a => a.Value.HasTraitInfo<RepairsVehiclesInfo>()).Select(e => e.Key).ToArray();
		}

		protected override void Created(Actor self)
		{
			this.queues = self.Owner.PlayerActor.TraitsImplementing<ProductionQueue>().ToArray();
			this.resources = self.Owner.PlayerActor.TraitsImplementing<PlayerResources>().FirstOrDefault();
		}

		void IBotTick.BotTick(IBot bot)
		{
			// For performance we delay some ai tasks => OpenKrush runs with 25 ticks per second (at normal speed).
			if (bot.Player.World.WorldTick % 25 != 0)
				return;

			this.HandleBuildings(bot);
			this.HandleTowers(bot);
		}

		private void HandleBuildings(IBot bot)
		{
			if (this.resources == null || this.resources.Cash == 0)
				return;

			var queue = this.queues.FirstOrDefault(q => q.Info.Type == "building");

			if (queue == null)
				return;

			var buildings = bot.Player.World.Actors.Where(
					a => a.Owner == bot.Player && (a.Info.TraitInfoOrDefault<TechLevelBuildableInfo>()?.Queue.Contains("building") ?? false)
				)
				.ToArray();

			var constructedBuildings = buildings.Where(
					building =>
					{
						var selfConstructing = building.TraitOrDefault<SelfConstructing>();

						return selfConstructing is not { IsConstructing: true };
					}
				)
				.ToArray();

			if (!constructedBuildings.Any())
				return;

			var buildables = queue.BuildableItems().ToArray();

			// If we do not have a base, and we can place a base, we ALWAYS place it, regardless of anything else being build!
			if (!buildings.Any(building => this.bases.Contains(building.Info.Name)))
			{
				var actorInfo = buildables.FirstOrDefault(buildable => this.bases.Contains(buildable.Name));

				if (actorInfo != null)
				{
					this.PlaceConstruction(bot, actorInfo, PlacementType.NearBase, queue);

					return;
				}
			}

			// If we are constructing anything, do not place anything to avoid delaying build times.
			if (constructedBuildings.Length < buildings.Length)
				return;

			// If we have no or less power stations than the lowest building techlevel, build another one!
			var powerStations = buildings.Where(building => this.powerStations.Contains(building.Info.Name)).ToArray();
			var requiredPowerStations = Math.Max(1, !buildings.Any() ? 0 : buildings.Max(building => building.TraitOrDefault<Researchable>()?.Level ?? 0));

			if (powerStations.Length < requiredPowerStations)
			{
				var actorInfo = buildables.FirstOrDefault(buildable => this.powerStations.Contains(buildable.Name));

				if (actorInfo != null)
				{
					this.PlaceConstruction(bot, actorInfo, PlacementType.NearOil, queue);

					return;
				}
			}

			// Build missing factories.
			var requiredfactories = Math.Max(1, !buildings.Any() ? 0 : buildings.Max(building => building.TraitOrDefault<Researchable>()?.Level ?? 0));

			foreach (var factory in this.factories)
			{
				var factories = buildings.Where(building => building.Info.Name == factory).ToArray();

				if (factories.Length >= requiredfactories)
					continue;

				var actorInfo = buildables.FirstOrDefault(buildable => buildable.Name == factory);

				if (actorInfo == null)
					continue;

				this.PlaceConstruction(bot, actorInfo, PlacementType.NearBase, queue);

				return;
			}

			// Build any if possible in this order:
			var order = new[] { this.researchers, this.superWeapons, this.moneyGenerators };

			foreach (var category in order)
			{
				foreach (var actorInfo in buildables)
				{
					if (!category.Contains(actorInfo.Name))
						continue;

					this.PlaceConstruction(bot, actorInfo, PlacementType.NearBase, queue);

					return;
				}
			}

			// If the whole base is set up, also make sure we have at last one repairer.
			var repairer = buildables.FirstOrDefault(buildable => this.repairers.Contains(buildable.Name));

			if (repairer == null || buildings.Any(building => building.Info == repairer))
				return;

			this.PlaceConstruction(bot, repairer, PlacementType.NearOil, queue);
		}

		private void HandleTowers(IBot bot)
		{
			if (this.resources == null || this.resources.Cash == 0)
				return;

			var queue = this.queues.FirstOrDefault(q => q.Info.Type == "tower");

			if (queue == null)
				return;

			var towers = bot.Player.World.Actors
				.Where(a => a.Owner == bot.Player && (a.Info.TraitInfoOrDefault<TechLevelBuildableInfo>()?.Queue.Contains("tower") ?? false))
				.ToArray();

			var constructedTowers = towers.Where(
					building =>
					{
						var selfConstructing = building.TraitOrDefault<SelfConstructing>();

						return selfConstructing is not { IsConstructing: true };
					}
				)
				.ToArray();

			if (constructedTowers.Length < towers.Length)
				return;

			var tower = queue.BuildableItems().OrderBy(info => info.TraitInfoOrDefault<TechLevelBuildableInfo>()?.Level ?? 0).FirstOrDefault();

			if (tower == null)
				return;

			this.PlaceConstruction(bot, tower, PlacementType.NearEnemy, queue);
		}

		private void PlaceConstruction(IBot bot, ActorInfo actorInfo, PlacementType type, ProductionQueue queue)
		{
			var placeLocation = this.ChooseBuildTarget(
				bot.Player.World,
				bot.Player,
				actorInfo.TraitInfoOrDefault<RequiresBuildableAreaInfo>()?.Adjacent ?? 0,
				actorInfo,
				type
			);

			if (placeLocation == null)
				return;

			bot.QueueOrder(
				new("PlaceBuilding", bot.Player.PlayerActor, Target.FromCell(bot.Player.World, placeLocation.Value), false)
				{
					TargetString = actorInfo.Name, ExtraData = queue.Actor.ActorID, SuppressVisualFeedback = true
				}
			);
		}

		private CPos? ChooseBuildTarget(World world, Player player, int maxRange, ActorInfo actorInfo, PlacementType type)
		{
			var requiredQueue = actorInfo.TraitInfoOrDefault<RequiresBuildableAreaInfo>()?.AreaTypes.FirstOrDefault();

			if (requiredQueue == null)
				return null;

			var sources = world.ActorsWithTrait<GivesBuildableArea>()
				.Where(e => e.Actor.Owner == player && !e.Trait.IsTraitDisabled)
				.Select(e => e.Actor)
				.ToArray();

			if (sources.Length == 0)
				return null;

			var buildingInfo = actorInfo.TraitInfoOrDefault<BuildingInfo>();

			var cells = sources.SelectMany(building => world.Map.FindTilesInAnnulus(building.Location, 0, maxRange + buildingInfo.Dimensions.Length))
				.Distinct()
				.Where(cell => world.CanPlaceBuilding(cell, actorInfo, buildingInfo, null))
				.ToArray();

			var targets = new List<CPos>();

			switch (type)
			{
				case PlacementType.NearBase:
					targets.AddRange(world.Actors.Where(a => this.bases.Contains(a.Info.Name) && a.Owner == player).Select(e => e.Location));

					break;

				case PlacementType.NearEnemy:
					targets.AddRange(
						world.Actors.Where(a => this.bases.Contains(a.Info.Name) && a.Owner.RelationshipWith(player) == PlayerRelationship.Enemy)
							.Select(e => e.Location)
					);

					break;

				case PlacementType.NearOil:
					// TODO this should be used to place power stations.
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}

			if (targets.Count == 0)
				targets.AddRange(sources.Select(source => source.Location));

			if (targets.Count == 0)
				return null;

			var target = targets.Random(world.LocalRandom);

			return cells.Where(
					cell =>
					{
						for (var y = -1; y <= 1; y++)
						for (var x = -1; x <= 1; x++)
						{
							if (!cells.Contains(cell + new CVec(x, y)))
								return false;
						}

						return true;
					}
				)
				.OrderBy(c => (c - target).LengthSquared)
				.FirstOrDefault();
		}
	}
}

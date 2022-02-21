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
	using OpenRA.Traits;

	[UsedImplicitly]
	public class BotBaseBuilderInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init)
		{
			return new BotBaseBuilder(init.World, this);
		}
	}

	public class BotBaseBuilder : ConditionalTrait<BotBaseBuilderInfo>, IBotTick, INotifyCreated
	{
		private readonly IEnumerable<string> bases;
		private ProductionQueue[] queues = Array.Empty<ProductionQueue>();
		private PlayerResources? resources;

		public BotBaseBuilder(World world, BotBaseBuilderInfo info)
			: base(info)
		{
			this.bases = world.Map.Rules.Actors.Where(a => a.Value.HasTraitInfo<BaseBuildingInfo>()).Select(e => e.Key);
		}

		void INotifyCreated.Created(Actor self)
		{
			this.queues = self.Owner.PlayerActor.TraitsImplementing<ProductionQueue>().ToArray();
			this.resources = self.Owner.PlayerActor.TraitsImplementing<PlayerResources>().FirstOrDefault();
		}

		void IBotTick.BotTick(IBot bot)
		{
			// For performance we delay some ai tasks => OpenKrush runs with 25 ticks per second (at normal speed).

			if (bot.Player.World.WorldTick % 25 == 0)
				this.HandleBuildings(bot);
		}

		private void HandleBuildings(IBot bot)
		{
			if (this.resources == null || this.resources.Cash == 0)
				return;

			var queue = this.queues.FirstOrDefault(q => q.Info.Type == "building");

			if (queue == null)
				return;

			var buildings = bot.Player.World.Actors.Where(a => a.Owner == bot.Player && a.Info.HasTraitInfo<BuildingInfo>()).ToArray();

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
				var baseActorInfo = buildables.FirstOrDefault(buildable => this.bases.Contains(buildable.Name));

				if (baseActorInfo != null)
				{
					this.PlaceConstruction(bot, constructedBuildings, baseActorInfo, PlacementType.NearBase, queue);

					return;
				}
			}

			// If we are constructing anything, do not place anything to avoid delaying build times.
			if (constructedBuildings.Length < buildings.Length)
				return;

			// TODO continue to build base in order with priority!
			var randomBuilding = buildables.Where(b => !this.bases.Contains(b.Name)).RandomOrDefault(bot.Player.World.LocalRandom);

			if (randomBuilding != null)
				this.PlaceConstruction(bot, constructedBuildings, randomBuilding, PlacementType.NearBase, queue);
		}

		private void PlaceConstruction(IBot bot, Actor[] buildings, ActorInfo actorInfo, PlacementType type, ProductionQueue queue)
		{
			var placeLocation = this.ChooseBuildTarget(
				bot.Player.World,
				actorInfo.TraitInfoOrDefault<RequiresBuildableAreaInfo>()?.Adjacent ?? 0,
				buildings,
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

		private CPos? ChooseBuildTarget(World world, int maxRange, Actor[] buildings, ActorInfo actorInfo, PlacementType type)
		{
			var buildingInfo = actorInfo.TraitInfoOrDefault<BuildingInfo>();

			// TODO we should in general always make sure we have 2 tiles space between buildings!
			var cells = buildings.SelectMany(building => world.Map.FindTilesInAnnulus(building.Location, 0, maxRange))
				.Where(cell => world.CanPlaceBuilding(cell, actorInfo, buildingInfo, null));

			switch (type)
			{
				case PlacementType.NearBase:
				{
					var baseLocation = (buildings.FirstOrDefault(b => this.bases.Contains(b.Info.Name)) ?? buildings.FirstOrDefault())?.Location;

					if (baseLocation == null)
						break;

					return cells.OrderBy(c => (c - baseLocation.Value).LengthSquared).FirstOrDefault();
				}

				case PlacementType.NearOil:
					// TODO this should be used to place power stations.
					break;

				case PlacementType.NearEnemy:
					// TODO this should be used to place defences.
					break;

				default:
					throw new ArgumentOutOfRangeException(Enum.GetName(type));
			}

			return CPos.Zero;
		}
	}
}

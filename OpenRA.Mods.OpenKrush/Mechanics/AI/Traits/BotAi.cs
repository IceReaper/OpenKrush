#region Copyright & License Information

/*
 * Copyright 2007-2022 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Mechanics.AI.Traits;

using Common;
using Common.Activities;
using Common.Traits;
using Construction.Orders;
using Construction.Traits;
using JetBrains.Annotations;
using Misc.Traits;
using Oil.Traits;
using OpenRA.Traits;
using Production.Traits;
using Repairbays.Traits;
using Researching.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class BotAiInfo : ConditionalTraitInfo
{
	// For performance we delay some ai tasks => OpenKrush runs with 25 ticks per second (at normal speed).
	public int ThinkDelay = 25;

	public override object Create(ActorInitializer init)
	{
		return new BotAi(init.World, this);
	}
}

public class BotAi : IBotTick
{
	private class OilPatchData
	{
		public readonly Actor OilPatch;
		public Actor? Derrick;
		public Actor? Drillrig;
		public Actor? PowerStation;
		public readonly List<Actor> Tankers = new();

		public OilPatchData(Actor oilPatch)
		{
			this.OilPatch = oilPatch;
		}
	}

	private class Sector
	{
		public readonly WPos Origin;
		public readonly List<OilPatchData> OilPatches = new();
		public bool Claimed;
		public Actor? MobileBase;

		public Sector(WPos origin)
		{
			this.Origin = origin;
		}
	}

	private readonly World world;
	private readonly BotAiInfo info;

	private Sector[] sectors = Array.Empty<Sector>();
	private bool initialized;
	private int thinkDelay;

	public BotAi(World world, BotAiInfo info)
	{
		this.world = world;
		this.info = info;
	}

	void IBotTick.BotTick(IBot bot)
	{
		if (!this.initialized)
			this.Initialize();

		this.thinkDelay = ++this.thinkDelay % this.info.ThinkDelay;

		if (this.thinkDelay != 0)
			return;

		this.UpdateSectorsClaims(bot);
		this.HandleMobileBases(bot);
		this.HandleMobileDerricks(bot);
		this.AssignOilActors(bot);
		this.ConstructBuildings(bot);
		this.SellBuildings(bot);
	}

	private void Initialize()
	{
		this.sectors = this.world.Actors.Where(actor => actor.Info.Name == "mpspawn").Select(actor => new Sector(actor.CenterPosition)).ToArray();

		foreach (var oilPatch in this.world.ActorsHavingTrait<OilPatch>())
		{
			var distanceToNearestSector = this.sectors.Min(sector => (sector.Origin - oilPatch.CenterPosition).Length);

			foreach (var sector in this.sectors.Where(sector => (sector.Origin - oilPatch.CenterPosition).Length == distanceToNearestSector))
				sector.OilPatches.Add(new(oilPatch));
		}

		Console.WriteLine(this.world.Map.Title);

		foreach (var sector in this.sectors)
		{
			Console.WriteLine(
				$"- Registered Sector [{sector.Origin.X},{sector.Origin.Y}] with {sector.OilPatches.Count} Oilpatches ({sector.OilPatches.Count(oilPatch => this.sectors.Count(s => s.OilPatches.Contains(oilPatch)) > 1)} shared)"
			);
		}

		this.initialized = true;
	}

	private void UpdateSectorsClaims(IBot bot)
	{
		var buildingsInSectors = this.world.ActorsHavingTrait<ProvidesPrerequisite>()
			.Where(actor => actor.Owner == bot.Player)
			.GroupBy(actor => this.sectors.MinBy(sector => (sector.Origin - actor.CenterPosition).Length))
			.ToDictionary(e => e.Key, e => e);

		foreach (var sector in this.sectors)
		{
			sector.Claimed = buildingsInSectors.ContainsKey(sector);

			foreach (var oilPatch in sector.OilPatches.ToArray())
			{
				if (oilPatch.OilPatch is { IsDead: true })
				{
					sector.OilPatches.Remove(oilPatch);

					continue;
				}

				if (!sector.Claimed || oilPatch.Drillrig is { IsDead: true })
					oilPatch.Drillrig = null;

				if (!sector.Claimed || oilPatch.PowerStation is { IsDead: true })
					oilPatch.PowerStation = null;

				oilPatch.Tankers.RemoveAll(tanker => !sector.Claimed || tanker.IsDead);
			}
		}
	}

	private void SellBuildings(IBot bot)
	{
		var sellables = this.world.ActorsWithTrait<DeconstructSellable>()
			.Where(e => e.Actor.Owner == bot.Player && !e.Trait.IsTraitDisabled)
			.Select(e => e.Actor);

		foreach (var sellable in sellables)
		{
			if (!this.sectors.MinBy(sector => (sector.Origin - sellable.CenterPosition).Length).Claimed)
				bot.QueueOrder(new(SellOrderGenerator.Id, sellable, false));

			if (sellable.TraitOrDefault<Drillrig>() != null && !this.sectors.Any(s => s.OilPatches.Any(oilPatch => sellable == oilPatch.Drillrig)))
				bot.QueueOrder(new(SellOrderGenerator.Id, sellable, false));
		}
	}

	private void HandleMobileBases(IBot bot)
	{
		var mobileBases = this.world.ActorsHavingTrait<BaseBuilding>().Where(actor => actor.TraitsImplementing<Mobile>().Any() && actor.Owner == bot.Player);

		foreach (var sector in this.sectors)
		{
			if (sector.MobileBase is { IsDead: true })
				sector.MobileBase = null;
		}

		foreach (var mobileBase in mobileBases)
		{
			if (this.sectors.Any(sector => mobileBase == sector.MobileBase))
				continue;

			var sector = this.sectors.Where(s => s is { Claimed: false, MobileBase: null }).MinByOrDefault(s => (s.Origin - mobileBase.CenterPosition).Length);

			if (sector == null)
				break;

			sector.MobileBase = mobileBase;
		}

		foreach (var sector in this.sectors)
		{
			if (sector.MobileBase is not { IsIdle: true })
				continue;

			sector.MobileBase.QueueActivity(
				sector.Origin == sector.MobileBase.CenterPosition
					? sector.MobileBase.Trait<Transforms>().GetTransformActivity()
					: new Move(sector.MobileBase, this.world.Map.CellContaining(sector.Origin))
			);
		}
	}

	private void HandleMobileDerricks(IBot bot)
	{
		var derricks = this.world.ActorsHavingTrait<DeploysOnActor>().Where(actor => actor.Owner == bot.Player);

		foreach (var oilPatch in this.sectors.SelectMany(sector => sector.OilPatches.Where(op => !sector.Claimed || op.Derrick is { IsDead: true })))
			oilPatch.Derrick = null;

		foreach (var derrick in derricks)
		{
			if (this.sectors.Any(sector => sector.OilPatches.Any(oilPatch => derrick == oilPatch.Derrick)))
				continue;

			var oilPatch = this.sectors.Where(sector => sector.Claimed)
				.MinByOrDefault(sector => (sector.Origin - derrick.CenterPosition).Length)
				?.OilPatches.Where(oilPatch => oilPatch.Derrick == null && oilPatch.Drillrig == null)
				.MinByOrDefault(oilPatch => (oilPatch.OilPatch.CenterPosition - derrick.CenterPosition).Length);

			if (oilPatch == null)
				continue;

			oilPatch.Derrick = derrick;
		}

		foreach (var oilPatch in this.sectors.SelectMany(sector => sector.OilPatches))
		{
			if (oilPatch.Derrick is { IsIdle: true } && oilPatch.OilPatch.CenterPosition != oilPatch.Derrick.CenterPosition)
				oilPatch.Derrick.QueueActivity(new Move(oilPatch.Derrick, this.world.Map.CellContaining(oilPatch.OilPatch.CenterPosition)));
		}
	}

	private void AssignOilActors(IBot bot)
	{
		var drillrigs = this.world.ActorsHavingTrait<Drillrig>().Where(actor => actor.Owner == bot.Player).ToArray();
		var powerStations = this.world.ActorsHavingTrait<PowerStation>().Where(actor => actor.Owner == bot.Player).ToList();

		foreach (var sector in this.sectors.Where(sector => sector.Claimed))
		{
			foreach (var oilPatch in sector.OilPatches)
			{
				oilPatch.Drillrig ??= drillrigs.FirstOrDefault(drillrig => (drillrig.CenterPosition - oilPatch.OilPatch.CenterPosition).Length < WDist.FromCells(1).Length);

				if (oilPatch.PowerStation != null)
					powerStations.Remove(oilPatch.PowerStation);
			}
		}
		
		foreach (var powerStation in powerStations)
		{
			var oilPatch = this.sectors.Where(sector => sector.Claimed)
				.MinByOrDefault(sector => (sector.Origin - powerStation.CenterPosition).Length)
				?.OilPatches.Where(oilPatch => oilPatch is { Drillrig: { }, PowerStation: null })
				.MinByOrDefault(oilPatch => (oilPatch.OilPatch.CenterPosition - powerStation.CenterPosition).Length);

			if (oilPatch != null)
				oilPatch.PowerStation = powerStation;
		}
	}

	private void ConstructBuildings(IBot bot)
	{
		var productionQueue = this.world.ActorsWithTrait<SelfConstructingProductionQueue>()
			.FirstOrDefault(e => e.Actor.Owner == bot.Player && e.Trait.Info.Type == "building")
			.Trait;

		if (productionQueue == null)
			return;

		var buildables = productionQueue.BuildableItems().ToArray();

		var sectorOrder = this.sectors.Where(sector => sector.Claimed);

		var buildingsInSectors = this.world.ActorsHavingTrait<ProvidesPrerequisite>()
			.Where(actor => actor.Owner == bot.Player)
			.GroupBy(actor => this.sectors.MinBy(sector => (sector.Origin - actor.CenterPosition).Length))
			.ToDictionary(e => e.Key, e => e);

		foreach (var sector in sectorOrder)
		{
			if (!buildingsInSectors.TryGetValue(sector, out var buildingsInSector))
				continue;

			var build = buildables.Where(buildable => buildable.HasTraitInfo<BaseBuildingInfo>())
				.FirstOrDefault(buildable => buildingsInSector.All(building => building.Info.Name != buildable.Name));

			if (this.Build(bot, sector, build, productionQueue))
				continue;

			if (productionQueue.IsConstructing())
				continue;

			var oilPatch = sector.OilPatches.Where(oilPatch => oilPatch is { Drillrig: { }, PowerStation: null })
				.MinByOrDefault(oilPatch => (oilPatch.OilPatch.CenterPosition - sector.Origin).Length);

			if (oilPatch != null)
			{
				build = buildables.Where(buildable => buildable.HasTraitInfo<PowerStationInfo>())
					.FirstOrDefault(buildable => buildingsInSector.All(building => building.Info.Name != buildable.Name));

				if (this.Build(bot, sector, build, productionQueue, oilPatch.OilPatch.CenterPosition))
					break;
			}

			build = buildables.Where(buildable => buildable.HasTraitInfo<AdvancedProductionInfo>())
				.FirstOrDefault(buildable => buildingsInSector.All(building => building.Info.Name != buildable.Name));

			if (this.Build(bot, sector, build, productionQueue))
				break;

			build = buildables.Where(buildable => buildable.HasTraitInfo<ResearchesInfo>())
				.FirstOrDefault(buildable => buildingsInSector.All(building => building.Info.Name != buildable.Name));

			if (this.Build(bot, sector, build, productionQueue))
				break;

			build = buildables.Where(buildable => buildable.HasTraitInfo<CashTricklerInfo>())
				.FirstOrDefault(buildable => buildingsInSector.All(building => building.Info.Name != buildable.Name));

			if (this.Build(bot, sector, build, productionQueue))
				break;

			build = buildables.Where(buildable => buildable.HasTraitInfo<AdvancedAirstrikePowerInfo>())
				.FirstOrDefault(buildable => buildingsInSector.All(building => building.Info.Name != buildable.Name));

			if (this.Build(bot, sector, build, productionQueue))
				break;

			build = buildables.Where(buildable => buildable.HasTraitInfo<AdvancedProductionInfo>())
				.FirstOrDefault(
					buildable =>
					{
						var existing = buildingsInSector.Where(building => building.Info.Name == buildable.Name).ToArray();

						if (existing.Length != 1)
							return false;

						var researchable = existing.First().TraitOrDefault<Researchable>();

						return researchable == null || researchable.Level == researchable.MaxLevel;
					}
				);

			if (this.Build(bot, sector, build, productionQueue))
				break;

			build = buildables.Where(buildable => buildable.HasTraitInfo<RepairsVehiclesInfo>())
				.FirstOrDefault(buildable => buildingsInSector.All(building => building.Info.Name != buildable.Name));

			if (this.Build(bot, sector, build, productionQueue))
				break;
		}
	}

	private bool Build(IBot bot, Sector sector, ActorInfo? buildable, ProductionQueue queue, WPos? target = null)
	{
		if (buildable == null)
			return false;

		var buildingInfo = buildable.TraitInfoOrDefault<BuildingInfo>();

		if (buildingInfo == null)
			return true;

		var center = this.world.Map.CellContaining(sector.Origin);
		var buildTarget = target == null ? center : this.world.Map.CellContaining(target.Value);
		var minRange = 0;
		var maxRange = this.sectors.Where(other => other != sector).Min(other => (other.Origin - sector.Origin).Length) / 1024 / 2;

		var cells = this.world.Map.FindTilesInAnnulus(center, minRange, maxRange);

		cells = center != buildTarget ? cells.OrderBy(c => (c - buildTarget).LengthSquared) : cells.Shuffle(this.world.LocalRandom);

		foreach (var cell in cells)
		{
			if (!this.world.CanPlaceBuilding(cell, buildable, buildingInfo, null))
				continue;

			if (!buildingInfo.IsCloseEnoughToBase(this.world, bot.Player, buildable, cell))
				continue;

			bot.QueueOrder(
				new("PlaceBuilding", bot.Player.PlayerActor, Target.FromCell(this.world, cell), false)
				{
					TargetString = buildable.Name, ExtraData = queue.Actor.ActorID, SuppressVisualFeedback = true
				}
			);

			break;
		}

		return true;
	}
}

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

using Common.Activities;
using Common.Traits;
using Construction.Orders;
using Construction.Traits;
using JetBrains.Annotations;
using Oil.Traits;
using OpenRA.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class BotAiInfo : ConditionalTraitInfo
{
	public WDist DeployRadius;

	public override object Create(ActorInitializer init)
	{
		return new BotAi(init.World, this);
	}
}

public class BotAi : IBotTick
{
	private enum Claim
	{
		Unclaimed,
		Primary,
		Secondary
	}

	private class Sector
	{
		public readonly WPos Origin;
		public readonly List<Actor> OilPatches = new();
		public Claim Claim = Claim.Unclaimed;

		public Sector(WPos origin)
		{
			this.Origin = origin;
		}
	}

	private readonly World world;
	private readonly BotAiInfo info;

	private Sector[] sectors = Array.Empty<Sector>();
	private bool initialized;

	public BotAi(World world, BotAiInfo info)
	{
		this.world = world;
		this.info = info;
	}

	public void BotTick(IBot bot)
	{
		if (!this.initialized)
			this.Initialize();

		this.UpdateSectorOilpatches();
		this.UpdateSectorClaims(bot);
		this.SellEverythingInUnclaimedSectors(bot);
		this.HandleMobileBases(bot);
		this.HandleMobileDerricks(bot);
	}

	private void Initialize()
	{
		this.sectors = this.world.Actors.Where(actor => actor.Info.Name == "mpspawn").Select(actor => new Sector(actor.CenterPosition)).ToArray();

		var oilPatches = this.world.ActorsHavingTrait<OilPatch>();

		foreach (var oilPatch in oilPatches)
		{
			var distanceToNearestSector = this.sectors.Min(sector => (sector.Origin - oilPatch.CenterPosition).Length);

			var assignToSectors = this.sectors.Where(sector => (sector.Origin - oilPatch.CenterPosition).Length == distanceToNearestSector);

			foreach (var sector in assignToSectors)
				sector.OilPatches.Add(oilPatch);
		}

		Console.WriteLine(this.world.Map.Title);

		foreach (var sector in this.sectors)
		{
			Console.WriteLine(
				$"- Registered Sector [{sector.Origin.X},{sector.Origin.Y}] with {sector.OilPatches.Count} Oilpatches ({sector.OilPatches.Count(oilPatch => this.sectors.Count(sector => sector.OilPatches.Contains(oilPatch)) > 1)} shared)"
			);
		}

		this.initialized = true;
	}

	private void UpdateSectorOilpatches()
	{
		foreach (var sector in this.sectors)
			sector.OilPatches.RemoveAll(oilPatch => oilPatch.IsDead);
	}

	private void UpdateSectorClaims(IBot bot)
	{
		var buildingsInSectors = this.world.ActorsHavingTrait<ProvidesPrerequisite>()
			.Where(actor => actor.Owner == bot.Player)
			.GroupBy(actor => this.sectors.MinBy(sector => (sector.Origin - actor.CenterPosition).Length))
			.ToDictionary(e => e.Key, e => e);

		foreach (var sector in this.sectors)
		{
			if (buildingsInSectors.ContainsKey(sector) && buildingsInSectors[sector].Any())
			{
				if (sector.Claim == Claim.Unclaimed)
					sector.Claim = this.sectors.Any(sector => sector.Claim == Claim.Primary) ? Claim.Secondary : Claim.Primary;
			}
			else
				sector.Claim = Claim.Unclaimed;
		}

		if (this.sectors.Any(sector => sector.Claim == Claim.Primary))
			return;

		var newPrimarySector = this.sectors.FirstOrDefault(sector => sector.Claim == Claim.Secondary);

		if (newPrimarySector != null)
			newPrimarySector.Claim = Claim.Primary;
	}

	private void SellEverythingInUnclaimedSectors(IBot bot)
	{
		var buildingsToSell = this.world.ActorsWithTrait<DeconstructSellable>()
			.Where(
				e => e.Actor.Owner == bot.Player
					&& !e.Trait.IsTraitDisabled
					&& this.sectors.MinBy(sector => (sector.Origin - e.Actor.CenterPosition).Length).Claim == Claim.Unclaimed
			)
			.Select(e => e.Actor);

		foreach (var building in buildingsToSell)
			bot.QueueOrder(new(SellOrderGenerator.Id, building, false));
	}

	private void HandleMobileBases(IBot bot)
	{
		var mobileBases = this.world.ActorsHavingTrait<BaseBuilding>().Where(actor => actor.TraitsImplementing<Mobile>().Any() && actor.Owner == bot.Player);

		var reservedSectors = new List<Sector>();
		var idleMobileBases = new List<Actor>();

		foreach (var mobileBase in mobileBases)
		{
			if (mobileBase.CurrentActivity is Move move)
			{
				var target = move.GetTargets(mobileBase).FirstOrDefault();

				if (target.Type != TargetType.Invalid)
					reservedSectors.Add(this.sectors.MinBy(sector => (sector.Origin - target.CenterPosition).Length));
			}
			else if (!mobileBase.IsIdle)
				reservedSectors.Add(this.sectors.MinBy(sector => (sector.Origin - mobileBase.CenterPosition).Length));
			else
				idleMobileBases.Add(mobileBase);
		}

		foreach (var mobileBase in idleMobileBases)
		{
			var targetSector = this.sectors.Where(sector => !reservedSectors.Contains(sector))
				.MinByOrDefault(sector => (sector.Origin - mobileBase.CenterPosition).Length);

			if (targetSector == null)
				break;

			reservedSectors.Add(targetSector);

			mobileBase.QueueActivity(
				(targetSector.Origin - mobileBase.CenterPosition).Length <= this.info.DeployRadius.Length
					? mobileBase.Trait<Transforms>().GetTransformActivity()
					: new Move(mobileBase, mobileBase.World.Map.CellContaining(targetSector.Origin))
			);
		}
	}

	private void HandleMobileDerricks(IBot bot)
	{
		var derricks = this.world.ActorsHavingTrait<DeploysOnActor>().Where(actor => actor.Owner == bot.Player);
		var allOilPatches = this.world.ActorsWithTrait<OilPatch>().Where(e => e.Trait.Drillrig == null).Select(e => e.Actor).ToArray();

		var reservedOilpatches = new List<Actor>();
		var idleDerricks = new List<Actor>();

		foreach (var derrick in derricks)
		{
			if (derrick.CurrentActivity is Move move)
			{
				var target = move.GetTargets(derrick).FirstOrDefault();

				if (target.Type == TargetType.Invalid)
					continue;

				var targetOilPatch = allOilPatches.FirstOrDefault(oilPatch => (oilPatch.CenterPosition - target.CenterPosition).Length == 0);

				if (targetOilPatch != null)
					reservedOilpatches.Add(targetOilPatch);
			}
			else if (!derrick.IsIdle)
			{
				var targetOilPatch = allOilPatches.FirstOrDefault(oilPatch => (oilPatch.CenterPosition - derrick.CenterPosition).Length == 0);

				if (targetOilPatch != null)
					reservedOilpatches.Add(targetOilPatch);
			}
			else
				idleDerricks.Add(derrick);
		}

		foreach (var derrick in idleDerricks)
		{
			var oilPatches = this.sectors.MinBy(sector => (sector.Origin - derrick.CenterPosition).Length).OilPatches;

			var targetOilpatch = oilPatches.Where(oilPatch => !reservedOilpatches.Contains(oilPatch))
				.MinByOrDefault(oilPatch => (oilPatch.CenterPosition - derrick.CenterPosition).Length);

			if (targetOilpatch == null)
				continue;

			reservedOilpatches.Add(targetOilpatch);
			
			if (targetOilpatch.CenterPosition != derrick.CenterPosition)
				derrick.QueueActivity(new Move(derrick, derrick.World.Map.CellContaining(targetOilpatch.CenterPosition)));
		}
	}
}

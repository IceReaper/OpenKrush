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

namespace OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits;

using Common.Traits;
using Docking.Traits;
using JetBrains.Annotations;
using OpenRA.Traits;
using Researching;
using Researching.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("PowerStation implementation.")]
public class PowerStationInfo : DockActionInfo, Requires<ResearchableInfo>
{
	public const string Prefix = "POWERSTATION::";

	[Desc("How many oil per tick should be pumped.")]
	public readonly int Amount = 20;

	[Desc("How many additional oil is given for free per pump.")]
	public readonly int[] Additional = { 0, 3, 6, 9, 12, 15 };

	[Desc("How many ticks to wait between pumps.")]
	public readonly int Delay = 6;

	public override object Create(ActorInitializer init)
	{
		return new PowerStation(init, this);
	}
}

public class PowerStation : DockAction, IProvidesResearchables
{
	private readonly PowerStationInfo info;

	private readonly Researchable researchable;

	public PowerStation(ActorInitializer init, PowerStationInfo info)
		: base(info)
	{
		this.info = info;
		this.researchable = init.Self.TraitOrDefault<Researchable>();
	}

	public override bool CanDock(Actor self, Actor target)
	{
		if (!target.Info.HasTraitInfo<TankerInfo>())
			return false;

		// Allow to give resources to allies too.
		return target.Owner.RelationshipWith(self.Owner) == PlayerRelationship.Ally;
	}

	public override bool Process(Actor self, Actor actor)
	{
		var tanker = actor.TraitOrDefault<Tanker>();

		if (tanker == null)
			return true;

		if (self.World.WorldTick % this.info.Delay == 0)
		{
			self.Owner.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(tanker.Pull(this.info.Amount) + this.info.Additional[this.researchable.Level]);
		}

		return tanker.Current == 0;
	}

	public Dictionary<string, int> GetResearchables(Actor self)
	{
		var technologies = new Dictionary<string, int>();

		for (var i = 0; i < this.info.Additional.Length; i++)
			technologies.Add(PowerStationInfo.Prefix + i, i);

		return technologies;
	}
}

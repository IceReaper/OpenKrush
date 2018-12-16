#region Copyright & License Information
/*
 * Copyright 2016-2018 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Docking;
using OpenRA.Mods.Kknd.Traits.Research;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Resources
{
	[Desc("KKnD specific power station implementation.")]
	class PowerStationInfo : DockActionInfo, Requires<ResearchableInfo>
	{
		[Desc("How many oil per tick should be pumped.")]
		public readonly int Amount = 20;

		[Desc("How many additional oil is given for free per pump.")]
		public readonly int[] Additional = { 0, 3, 6, 9, 12, 15 };

		[Desc("How many ticks to wait between pumps.")]
		public readonly int Delay = 6;

		public override object Create(ActorInitializer init) { return new PowerStation(init, this); }
	}

	class PowerStation : DockAction
	{
		private readonly PowerStationInfo info;
		private readonly Actor self;

		private readonly Researchable researchable;

		public PowerStation(ActorInitializer init, PowerStationInfo info) : base(info)
		{
			this.info = info;
			self = init.Self;
			researchable = self.Trait<Researchable>();
		}

		public override bool CanDock(Actor target, Dockable dockable)
		{
			if (dockable == null && !target.Info.HasTraitInfo<TankerInfo>())
				return false;

			if (dockable != null && !(dockable is Tanker))
				return false;

			var tanker = dockable as Tanker;
			return tanker == null || tanker.IsValidPowerStation(self);
		}

		public override bool Process(Actor actor)
		{
			var tanker = actor.Trait<Tanker>();

			if (actor.World.WorldTick % info.Delay == 0)
				actor.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(tanker.Pull(info.Amount) + info.Additional[researchable.Level]);

			return tanker.Current == 0;
		}
	}
}

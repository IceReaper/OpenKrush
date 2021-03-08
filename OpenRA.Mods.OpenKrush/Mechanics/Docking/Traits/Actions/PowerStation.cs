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

using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits;
using OpenRA.Mods.OpenKrush.Mechanics.Researching.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenKrush.Mechanics.Docking.Traits.Actions
{
	[Desc("PowerStation implementation.")]
	public class PowerStationInfo : DockActionInfo, Requires<ResearchableInfo>
	{
		[Desc("How many oil per tick should be pumped.")]
		public readonly int Amount = 20;

		[Desc("How many additional oil is given for free per pump.")]
		public readonly int[] Additional = { 0, 3, 6, 9, 12, 15 };

		[Desc("How many ticks to wait between pumps.")]
		public readonly int Delay = 6;

		public override object Create(ActorInitializer init) { return new PowerStation(init, this); }
	}

	public class PowerStation : DockAction
	{
		private readonly PowerStationInfo info;
		private readonly Actor self;

		private readonly Researchable researchable;

		public PowerStation(ActorInitializer init, PowerStationInfo info)
			: base(info)
		{
			this.info = info;
			self = init.Self;
			researchable = self.Trait<Researchable>();
		}

		public override bool CanDock(Actor target)
		{
			if (!target.Info.HasTraitInfo<TankerInfo>())
				return false;

			// Allow to give resources to allies too.
			if (target.Owner.RelationshipWith(self.Owner) != PlayerRelationship.Ally)
				return false;

			return true;
		}

		public override bool Process(Actor actor)
		{
			var tanker = actor.TraitOrDefault<Tanker>();

			if (tanker == null)
				return true;

			if (self.World.WorldTick % info.Delay == 0)
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(tanker.Pull(info.Amount) + info.Additional[researchable.Level]);

			return tanker.Current == 0;
		}
	}
}

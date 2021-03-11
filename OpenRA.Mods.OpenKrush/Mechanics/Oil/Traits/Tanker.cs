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
using OpenRA.Mods.OpenKrush.Mechanics.Oil.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits
{
	[Desc("Tanker implementation.")]
	public class TankerInfo : DockableInfo
	{
		[Desc("Maximum oil a tanker can hold.")]
		public readonly int Capacity = 500;

		public override object Create(ActorInitializer init) { return new Tanker(init, this); }
	}

	public class Tanker : Dockable, IHaveOil, INotifyCreated, ITick
	{
		private readonly TankerInfo info;

		private Actor self;

		public Actor PreferedDrillrig;
		public Actor PreferedPowerStation;

		public Tanker(ActorInitializer init, TankerInfo info)
			: base(info)
		{
			this.info = info;
			self = init.Self;
		}

		void INotifyCreated.Created(Actor self)
		{
			self.World.AddFrameEndTask(world => self.QueueActivity(new TankerCycle(self, this)));
		}

		public int Current { get; private set; }
		public int Maximum => info.Capacity;

		public int Pull(int amount)
		{
			var pullAmount = Math.Min(amount, Current);
			Current -= pullAmount;
			return pullAmount;
		}

		public int Push(int amount)
		{
			var pushAmount = Math.Min(amount, Maximum - Current);
			Current += pushAmount;
			return amount - pushAmount;
		}

		public void AssignNearestDrillrig()
		{
			PreferedDrillrig = self.World.ActorsHavingTrait<Drillrig>()
				.Where(actor => actor.Owner == self.Owner && IsValidDrillrig(actor))
				.OrderBy(a => (self.CenterPosition - a.CenterPosition).Length)
				.FirstOrDefault();
		}

		private bool IsValidDrillrig(Actor actor)
		{
			if (actor == null || actor.IsDead || !actor.IsInWorld)
				return false;

			var dock = actor.TraitOrDefault<Dock>();
			var drillRig = actor.TraitOrDefault<Drillrig>();

			return dock != null && !dock.IsTraitDisabled && drillRig != null && !drillRig.IsTraitDisabled && drillRig.CanDock(self);
		}

		public void AssignNearestPowerStation()
		{
			PreferedPowerStation = self.World.ActorsHavingTrait<PowerStation>()
				.Where(actor => actor.Owner == self.Owner && IsValidPowerStation(actor))
				.OrderBy(a => (self.CenterPosition - a.CenterPosition).Length)
				.FirstOrDefault();
		}

		private bool IsValidPowerStation(Actor actor)
		{
			if (actor == null || actor.IsDead || !actor.IsInWorld || !actor.Owner.IsAlliedWith(self.Owner))
				return false;

			var dock = actor.TraitOrDefault<Dock>();
			var powerStation = actor.TraitOrDefault<PowerStation>();

			return dock != null && !dock.IsTraitDisabled && powerStation != null && !powerStation.IsTraitDisabled && powerStation.CanDock(self);
		}

		void ITick.Tick(Actor self)
		{
			if (PreferedDrillrig != null && !IsValidDrillrig(PreferedDrillrig))
				PreferedDrillrig = null;

			if (PreferedPowerStation != null && !IsValidPowerStation(PreferedPowerStation))
				PreferedPowerStation = null;
		}
	}
}

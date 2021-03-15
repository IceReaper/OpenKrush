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

namespace OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits
{
	using System;
	using Activities;
	using Docking.Activities;
	using Docking.Traits;
	using Docking.Traits.Actions;
	using OpenRA.Activities;
	using OpenRA.Traits;

	[Desc("Tanker implementation.")]
	public class TankerInfo : DockableInfo
	{
		[Desc("Maximum oil a tanker can hold.")]
		public readonly int Capacity = 500;

		public override object Create(ActorInitializer init)
		{
			return new Tanker(this);
		}
	}

	public class Tanker : Dockable, IHaveOil, INotifyCreated, ITick
	{
		private readonly TankerInfo info;

		public Actor PreferedDrillrig;
		public Actor PreferedPowerStation;

		public Tanker(TankerInfo info)
			: base(info)
		{
			this.info = info;
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

		protected override Activity GetDockingActivity(Actor self, Actor target, Dock dock)
		{
			if (target.TraitOrDefault<Drillrig>() != null)
			{
				PreferedDrillrig = target;
				var tankerCycle = new TankerCycle(self, this);
				tankerCycle.QueueChild(new Docking(self, PreferedDrillrig, PreferedDrillrig.Trait<Dock>()));

				return tankerCycle;
			}

			if (target.TraitOrDefault<PowerStation>() != null)
			{
				PreferedPowerStation = target;
				var tankerCycle = new TankerCycle(self, this);
				tankerCycle.QueueChild(new Docking(self, PreferedPowerStation, PreferedPowerStation.Trait<Dock>()));

				return tankerCycle;
			}

			return base.GetDockingActivity(self, target, dock);
		}

		void ITick.Tick(Actor self)
		{
			if (PreferedDrillrig != null && !OilUtils.IsUsable(PreferedDrillrig, PreferedDrillrig.Trait<Drillrig>()))
			{
				// When releasing the drillrig, we should also release the powerstation as another one might be the better pick.
				PreferedDrillrig = null;
				PreferedPowerStation = null;
			}

			if (PreferedPowerStation != null && PreferedPowerStation.Owner.RelationshipWith(self.Owner) != PlayerRelationship.Ally)
				PreferedPowerStation = null;

			if (PreferedPowerStation != null && !OilUtils.IsUsable(PreferedPowerStation, PreferedPowerStation.Trait<PowerStation>()))
				PreferedPowerStation = null;
		}
	}
}

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

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits.Docking;
using OpenRA.Mods.Kknd.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Resources
{
	[Desc("KKnD specific tanker implementation.")]
	class TankerInfo : DockableInfo
	{
		[Desc("Maximum oil a tanker can hold.")]
		public readonly int Capacity = 500;

		public override object Create(ActorInitializer init) { return new Tanker(init, this); }
	}

	class Tanker : Dockable, IHaveOil, INotifyCreated
	{
		private readonly TankerInfo info;

		private Actor self;
		public Actor Drillrig { get; set; }
		public Actor PowerStation { get; set; }

		public Tanker(ActorInitializer init, TankerInfo info) : base(info)
		{
			this.info = info;
			self = init.Self;
		}

		void INotifyCreated.Created(Actor self)
		{
			self.World.AddFrameEndTask(world => self.QueueActivity(new TankerCycle(self, this, null, null)));
		}

		public int Current { get; private set; }
		public int Maximum { get { return info.Capacity; } }

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
			Drillrig = self.World.ActorsHavingTrait<Drillrig>()
				.Where(actor => actor.Owner == self.Owner && IsValidDrillrig(actor))
				.OrderBy(a => (self.CenterPosition - a.CenterPosition).Length)
				.FirstOrDefault();
		}

		public bool IsValidDrillrig(Actor actor)
		{
			if (actor == null || actor.IsDead || !actor.IsInWorld)
				return false;

			var dock = actor.TraitOrDefault<Dock>();
			var drillRig = actor.TraitOrDefault<Drillrig>();

			if (dock == null || dock.IsTraitDisabled || drillRig == null || drillRig.IsTraitDisabled || drillRig.Current == 0)
				return false;

			return true;
		}

		public void AssignNearestPowerStation()
		{
			PowerStation = self.World.ActorsHavingTrait<PowerStation>()
				.Where(actor => actor.Owner == self.Owner && IsValidPowerStation(actor))
				.OrderBy(a => (self.CenterPosition - a.CenterPosition).Length)
				.FirstOrDefault();
		}

		public bool IsValidPowerStation(Actor actor)
		{
			if (actor == null || actor.IsDead || !actor.IsInWorld || !actor.Owner.IsAlliedWith(self.Owner))
				return false;

			var dock = actor.TraitOrDefault<Dock>();
			var powerStation = actor.TraitOrDefault<PowerStation>();

			if (dock == null || dock.IsTraitDisabled || powerStation == null || powerStation.IsTraitDisabled)
				return false;

			return true;
		}
	}
}

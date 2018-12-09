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

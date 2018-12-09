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
		public readonly int[] Additional = {0, 3, 6, 9, 12, 15};

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

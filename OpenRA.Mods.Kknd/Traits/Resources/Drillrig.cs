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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Traits.Docking;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Resources
{
	[Desc("KKnD specific drillrig logic.")]
	class DrillrigInfo : DockActionInfo
	{
		[GrantedConditionReference]
		[Desc("Condition, which will be granted if the Drillrig is not empty.")]
		public readonly string Condition = "HasOil";

		[Desc("How many oil per tick should be pumped.")]
		public readonly int Rate = 3;

		public override object Create(ActorInitializer init) { return new Drillrig(init, this); }
	}

	class Drillrig : DockAction, ITick, IHaveOil, INotifySold, INotifyKilled
	{
		private readonly DrillrigInfo info;
		private readonly Actor self;

		private Actor oilpatchActor;
		private Oilpatch oilpatch;

		ConditionManager conditionManager;
		int token = ConditionManager.InvalidConditionToken;

		public Drillrig(ActorInitializer init, DrillrigInfo info)
			: base(info)
		{
			this.info = info;
			self = init.Self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			var actors = self.World.FindActorsInCircle(self.CenterPosition, new WDist(1024)).Where(a => a.Info.HasTraitInfo<OilpatchInfo>()).ToArray();

			if (actors.Any())
			{
				oilpatchActor = actors.First();
				self.World.AddFrameEndTask(world => world.Remove(oilpatchActor));
				oilpatch = oilpatchActor.Trait<Oilpatch>();
			}

			conditionManager = self.Trait<ConditionManager>();

			if (oilpatch != null)
				token = conditionManager.GrantCondition(self, info.Condition);
		}

		public int Current { get { return oilpatch == null ? 0 : oilpatch.Current; } }
		public int Maximum { get { return oilpatch == null ? 1 : oilpatchActor.Info.TraitInfo<OilpatchInfo>().FullAmount; } }

		public override bool CanDock(Actor target, Dockable dockable)
		{
			if (dockable == null && !target.Info.HasTraitInfo<TankerInfo>())
				return false;

			if (dockable != null && !(dockable is Tanker))
				return false;

			var tanker = dockable as Tanker;
			return tanker == null || tanker.IsValidDrillrig(self);
		}

		public override bool Process(Actor actor)
		{
			var tanker = actor.Trait<Tanker>();

			if (oilpatch != null)
			{
				var amount = oilpatch.Pull(info.Rate);
				var remaining = tanker.Push(amount);
				oilpatch.Push(remaining);
			}

			return oilpatch == null || tanker.Current == tanker.Maximum;
		}

		public override void OnDock()
		{
			if (oilpatch != null && oilpatch.Current <= 2500)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", "DrillrigLow", self.Owner.Faction.InternalName);
		}

		void ITick.Tick(Actor self)
		{
			if (oilpatchActor == null)
				return;

			oilpatchActor.Tick();

			if (!oilpatchActor.IsDead)
				return;

			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", "DrillrigEmpty", self.Owner.Faction.InternalName);
			oilpatchActor = null;
			oilpatch = null;
			conditionManager.RevokeCondition(self, token);
			token = ConditionManager.InvalidConditionToken;
		}

		void INotifySold.Selling(Actor self) { }

		void INotifySold.Sold(Actor self)
		{
			if (oilpatchActor != null)
				this.self.World.AddFrameEndTask(world => world.Add(oilpatchActor));
		}

		public void Killed(Actor self, AttackInfo attack)
		{
			if (oilpatchActor != null)
				this.self.World.AddFrameEndTask(world => world.Add(oilpatchActor));
		}
	}
}

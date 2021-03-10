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

using System.Linq;
using OpenRA.Mods.OpenKrush.Mechanics.Oil;
using OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenKrush.Mechanics.Docking.Traits.Actions
{
	[Desc("Drillrig logic.")]
	public class DrillrigInfo : DockActionInfo
	{
		[GrantedConditionReference]
		[Desc("Condition, which will be granted if the Drillrig is not empty.")]
		public readonly string Condition = "HasOil";

		[Desc("How many oil per tick should be pumped.")]
		public readonly int Rate = 3;

		public override object Create(ActorInitializer init) { return new Drillrig(init, this); }
	}

	public class Drillrig : DockAction, ITick, IHaveOil, INotifyRemovedFromWorld
	{
		private readonly DrillrigInfo info;
		private readonly Actor self;

		private Actor oilpatchActor;
		private Oilpatch oilpatch;

		private int token = Actor.InvalidConditionToken;

		public int Current => oilpatch == null ? 0 : oilpatch.Current;
		public int Maximum => oilpatch == null ? 1 : oilpatchActor.Info.TraitInfo<OilpatchInfo>().FullAmount;

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

			if (!actors.Any())
				return;

			oilpatchActor = actors.First();
			self.World.AddFrameEndTask(world => world.Remove(oilpatchActor));
			oilpatch = oilpatchActor.Trait<Oilpatch>();
			oilpatch.StopBurning();

			token = self.GrantCondition(info.Condition);
		}

		public override bool CanDock(Actor target)
		{
			if (!target.Info.HasTraitInfo<TankerInfo>())
				return false;

			return oilpatch != null;
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
			// TODO unhardcode!
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

			// TODO unhardcode!
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", "DrillrigEmpty", self.Owner.Faction.InternalName);

			oilpatchActor = null;
			oilpatch = null;
			token = self.RevokeCondition(token);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (oilpatchActor != null)
				this.self.World.AddFrameEndTask(world => world.Add(oilpatchActor));
		}
	}
}

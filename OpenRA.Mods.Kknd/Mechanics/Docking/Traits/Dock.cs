#region Copyright & License Information
/*
 * Copyright 2007-2021 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Mechanics.Oil.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Docking.Traits
{
	public class DockInfo : ConditionalTraitInfo, Requires<DockActionInfo>
	{
		[Desc("Actual actor facing when docking.")]
		public readonly WAngle Angle = new WAngle(384);

		[Desc("Docking relative to the Actor center.")]
		public readonly WVec Position = WVec.Zero;

		[Desc("Vector by which the actor will be dragged when docking.")]
		public readonly WVec DragOffset = new WVec(-1048, 1048, 0);

		[Desc("In how many steps to perform the dragging?")]
		public readonly int DragLength = 32;

		[Desc("Distance a dockable has to reach to get queued.")]
		public readonly int QueueDistance = 3;

		[Desc("Name of this dock to assign dock actions to..")]
		public readonly string Name = "Dock";

		public override object Create(ActorInitializer init) { return new Dock(init, this); }
	}

	public class Dock : ConditionalTrait<DockInfo>, ITick
	{
		private readonly IEnumerable<DockAction> dockActions;
		private readonly List<Actor> queue = new List<Actor>();

		public int QueueLength => queue.Count;

		public Dock(ActorInitializer init, DockInfo info)
			: base(info)
		{
			dockActions = init.Self.TraitsImplementing<DockAction>().Where(dockAction => dockAction.Info.Name == info.Name);
		}

		public DockAction GetDockAction(Actor target)
		{
			return dockActions.FirstOrDefault(dockAction => !dockAction.IsTraitDisabled && dockAction.CanDock(target));
		}

		public void Add(Actor target)
		{
			queue.Add(target);
		}

		public void Remove(Actor target)
		{
			queue.Remove(target);
		}

		public void OnDock()
		{
			foreach (var action in dockActions.Where(dockAction => !dockAction.IsTraitDisabled))
				action.OnDock();
		}

		public void OnUndock()
		{
			foreach (var action in dockActions.Where(dockAction => !dockAction.IsTraitDisabled))
				action.OnUndock();
		}

		void ITick.Tick(Actor self)
		{
			queue.RemoveAll(actor =>
			{
				if (actor.IsDead || !actor.IsInWorld)
					return true;

				var docking = FindDockingActivity(actor.CurrentActivity);

				if (docking == null || docking.Dock != this)
					return true;

				if (IsTraitDisabled)
				{
					docking.Cancel(actor, true);
					return true;
				}

				if (GetDockAction(actor) == null)
				{
					docking.Cancel(actor, true);
					return true;
				}

				return false;
			});

			var target = queue.FirstOrDefault();

			if (target == null)
				return;

			var activity = FindDockingActivity(target.CurrentActivity);

			switch (activity.DockingState)
			{
				case DockingState.Waiting:
					activity.StartDocking();
					break;

				case DockingState.Docked:
					var actions = dockActions.Where(dockAction => !dockAction.IsTraitDisabled).ToArray();

					if (!actions.Any() || actions.Aggregate(true, (current, dockAction) => dockAction.Process(target) && current))
					{
						OnUndock();
						activity.StartUndocking();
					}

					break;
			}
		}

		private Activities.Docking FindDockingActivity(Activity activity)
		{
			if (activity is Activities.Docking docking)
				return docking;

			// TODO this is ugly, refactor this!
			if (activity is TankerCycle tankerCycle)
				return tankerCycle.DockingActivity;

			return null;
		}
	}
}

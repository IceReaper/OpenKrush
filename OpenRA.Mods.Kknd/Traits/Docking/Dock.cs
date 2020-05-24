#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Docking
{
	public class DockInfo : ConditionalTraitInfo, Requires<DockActionInfo>
	{
		[Desc("Location relativ to the Actor center to dock.")]
		public readonly WVec Position = WVec.Zero;

		[Desc("Direction this dock can be entered from.")]
		public readonly int Facing = 96;

		[Desc("Distance a dockable has to reach to get queued.")]
		public readonly int QueueDistance = 3;

		public override object Create(ActorInitializer init) { return new Dock(init, this); }
	}

	public class Dock : ConditionalTrait<DockInfo>, ITick
	{
		private readonly IEnumerable<DockAction> dockActions;
		private List<Actor> queue = new List<Actor>();

		public Dock(ActorInitializer init, DockInfo info) : base(info)
		{
			dockActions = init.Self.TraitsImplementing<DockAction>();
		}

		public DockAction GetDockAction(Actor target, Dockable dockable = null)
		{
			return dockActions.FirstOrDefault(dockAction => !dockAction.IsTraitDisabled && dockAction.CanDock(target, dockable));
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
				if (actor.IsDead || !actor.IsInWorld || !(actor.CurrentActivity is Activities.Docking))
					return true;

				var docking = actor.CurrentActivity as Activities.Docking;
				if (docking.Dock != this)
					return true;

				return false;
			});

			var target = queue.FirstOrDefault();
			if (target == null)
				return;

			var activity = (Activities.Docking)target.CurrentActivity;

			switch (activity.DockingState)
			{
				case DockingState.Waiting:
					activity.StartDocking();
					break;

				case DockingState.Docked:
					var actions = dockActions.Where(dockAction => !dockAction.IsTraitDisabled).ToArray();
					if (!actions.Any() || actions.Aggregate(true, (current, dockAction) => dockAction.Process(target) && current))
					{
						activity.StartUndocking();
						OnUndock();
					}

					break;
			}
		}
	}
}

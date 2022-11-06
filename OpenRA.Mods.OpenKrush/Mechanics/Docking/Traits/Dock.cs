#region Copyright & License Information

/*
 * Copyright 2007-2022 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Mechanics.Docking.Traits;

using Activities;
using Common.Traits;
using JetBrains.Annotations;
using OpenRA.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class DockInfo : ConditionalTraitInfo, Requires<DockActionInfo>
{
	[Desc("Actual actor facing when docking.")]
	public readonly WAngle Angle = new(384);

	[Desc("Docking relative to the Actor center.")]
	public readonly WVec Position = WVec.Zero;

	[Desc("Vector by which the actor will be dragged when docking.")]
	public readonly WVec DragOffset = new(-1048, 1048, 0);

	[Desc("In how many steps to perform the dragging?")]
	public readonly int DragLength = 32;

	[Desc("Distance a dockable has to reach to get queued.")]
	public readonly int QueueDistance = 3;

	[Desc("Name of this dock to assign dock actions to..")]
	public readonly string Name = "Dock";

	public override object Create(ActorInitializer init)
	{
		return new Dock(init, this);
	}
}

public class Dock : ConditionalTrait<DockInfo>, ITick
{
	private readonly IEnumerable<DockAction> dockActions;
	private readonly List<Actor> queue = new();

	public int QueueLength => this.queue.Count;

	public Dock(ActorInitializer init, DockInfo info)
		: base(info)
	{
		this.dockActions = init.Self.TraitsImplementing<DockAction>().Where(dockAction => dockAction.Info.Name == info.Name);
	}

	public DockAction? GetDockAction(Actor self, Actor target)
	{
		return this.dockActions.FirstOrDefault(dockAction => !dockAction.IsTraitDisabled && dockAction.CanDock(self, target));
	}

	public void Add(Actor target)
	{
		this.queue.Add(target);
	}

	public void Remove(Actor target)
	{
		this.queue.Remove(target);
	}

	public void OnDock(Actor self)
	{
		foreach (var action in this.dockActions.Where(dockAction => !dockAction.IsTraitDisabled))
			action.OnDock(self);
	}

	public void OnUndock()
	{
		foreach (var action in this.dockActions.Where(dockAction => !dockAction.IsTraitDisabled))
			action.OnUndock();
	}

	void ITick.Tick(Actor self)
	{
		this.queue.RemoveAll(
			actor =>
			{
				if (actor.IsDead || !actor.IsInWorld)
					return true;

				if (actor.CurrentActivity is not IDockingActivity docking)
					return true;

				if (docking.Dock != this)
					return true;

				if (this.IsTraitDisabled)
				{
					actor.CurrentActivity.Cancel(actor, true);

					return true;
				}

				if (this.GetDockAction(self, actor) != null)
					return false;

				actor.CurrentActivity.Cancel(actor, true);

				return true;
			}
		);

		var target = this.queue.FirstOrDefault();

		if (target?.CurrentActivity is not IDockingActivity activity)
			return;

		switch (activity.DockingState)
		{
			case DockingState.Waiting:
				activity.StartDocking();

				break;

			case DockingState.Docked:
				var actions = this.dockActions.Where(dockAction => !dockAction.IsTraitDisabled).ToArray();

				if (!actions.Any() || actions.Aggregate(true, (current, dockAction) => dockAction.Process(self, target) && current))
				{
					this.OnUndock();
					activity.StartUndocking();
				}

				break;

			case DockingState.None:
				break;

			case DockingState.Approaching:
				break;

			case DockingState.PrepareDocking:
				break;

			case DockingState.Docking:
				break;

			case DockingState.Undocking:
				break;

			default:
				throw new ArgumentOutOfRangeException(Enum.GetName(activity.DockingState));
		}
	}
}

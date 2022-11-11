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
using OpenRA.Activities;
using OpenRA.Traits;
using Orders;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class DockableInfo : TraitInfo
{
	[Desc("Voice to use when ordering to dock.")]
	[VoiceReference]
	public readonly string Voice = "Action";

	public override object Create(ActorInitializer init)
	{
		return new Dockable(this);
	}
}

public class Dockable : IIssueOrder, IResolveOrder, IOrderVoice
{
	private readonly DockableInfo info;

	public Dockable(DockableInfo info)
	{
		this.info = info;
	}

	IEnumerable<IOrderTargeter> IIssueOrder.Orders
	{
		get
		{
			yield return new DockOrderTargeter();
		}
	}

	public Order? IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
	{
		return order.OrderID == DockOrderTargeter.Id ? new Order(order.OrderID, self, target, queued) : null;
	}

	public void ResolveOrder(Actor self, Order order)
	{
		if (order.OrderString != DockOrderTargeter.Id)
			return;

		if (order.Target.Type != TargetType.Actor || order.Target.Actor == null)
			return;

		var dock = Enumerable.MinBy(
			order.Target.Actor.TraitsImplementing<Dock>().Where(d => d.GetDockAction(order.Target.Actor, self) != null),
			d => d.QueueLength
		);

		if (dock == null)
			return;

		self.QueueActivity(false, this.GetDockingActivity(self, order.Target.Actor, dock));
	}

	protected virtual Activity GetDockingActivity(Actor self, Actor target, Dock dock)
	{
		return new Docking(self, target, dock);
	}

	string? IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
	{
		return order.OrderString == DockOrderTargeter.Id ? this.info.Voice : null;
	}
}

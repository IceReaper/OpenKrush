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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.OpenKrush.Mechanics.Docking.Orders;
using OpenRA.Mods.OpenKrush.Mechanics.Docking.Traits.Actions;
using OpenRA.Mods.OpenKrush.Mechanics.Oil.Activities;
using OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenKrush.Mechanics.Docking.Traits
{
	public class DockableInfo : TraitInfo
	{
		[Desc("Voice to use when ordering to dock.")]
		[VoiceReference]
		public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new Dockable(this); }
	}

	public class Dockable : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly DockableInfo Info;

		public Dockable(DockableInfo info)
		{
			Info = info;
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders { get { yield return new DockOrderTargeter(); } }

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == DockOrderTargeter.Id ? new Order(order.OrderID, self, target, queued) : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != DockOrderTargeter.Id)
				return;

			if (order.Target.Type != TargetType.Actor || order.Target.Actor == null)
				return;

			var dock = order.Target.Actor.TraitsImplementing<Dock>().Where(d => d.GetDockAction(self) != null).OrderBy(d => d.QueueLength).FirstOrDefault();

			if (dock == null)
				return;

			// TODO this is ugly, refactor this!
			var tanker = self.TraitOrDefault<Tanker>();

			if (tanker != null)
			{
				if (order.Target.Actor.TraitOrDefault<Drillrig>() != null)
				{
					tanker.PreferedDrillrig = order.Target.Actor;
					self.QueueActivity(false, new TankerCycle(self, tanker));
					return;
				}

				if (order.Target.Actor.TraitOrDefault<PowerStation>() != null)
				{
					tanker.PreferedPowerStation = order.Target.Actor;
					self.QueueActivity(false, new TankerCycle(self, tanker));
					return;
				}
			}

			self.QueueActivity(false, new Activities.Docking(self, order.Target.Actor, dock));
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == DockOrderTargeter.Id ? Info.Voice : null;
		}
	}
}

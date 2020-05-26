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
using OpenRA.Mods.Kknd.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Docking
{
	public class DockableInfo : ITraitInfo
	{
		[Desc("Main activity to use when docking.")]
		public readonly string DockingActivity = "Docking";

		[Desc("Dock activity to use for docking and undocking.")]
		public readonly string DockingSequenceActivity = "Backwards";

		[Desc("Voice to use when ordering to dock.")]
		[VoiceReference]
		public readonly string Voice = "Action";

		public virtual object Create(ActorInitializer init) { return new Dockable(this); }
	}

	public class Dockable : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly DockableInfo Info;

		public Dockable(DockableInfo info)
		{
			Info = info;
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders { get { yield return new DockOrderTargeter(); } }

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			return order.OrderID == DockOrderTargeter.Id ? new Order(order.OrderID, self, target, queued) : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != DockOrderTargeter.Id)
				return;

			if (order.Target.Type != TargetType.Actor || order.Target.Actor == null)
				return;

			foreach (var dock in order.Target.Actor.TraitsImplementing<Dock>())
			{
				if (dock.GetDockAction(self, this) == null)
					continue;

				var activity = Game.ModData.ObjectCreator.CreateObject<Activities.Docking>(
					Info.DockingActivity, new Dictionary<string, object>
					{
						{ "dockableActor", self },
						{ "dockable", this },
						{ "dockActor", order.Target.Actor },
						{ "dock", dock }
					});

				self.QueueActivity(false, activity);
				break;
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == DockOrderTargeter.Id ? Info.Voice : null;
		}
	}
}

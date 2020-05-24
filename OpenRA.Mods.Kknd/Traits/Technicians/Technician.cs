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

using System.Collections.Generic;
using OpenRA.Mods.Kknd.Activities;
using OpenRA.Mods.Kknd.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Technicians
{
	[Desc("KKnD specific technician implementation.")]
	class TechnicianInfo : ITraitInfo
	{
		[Desc("How many ticks the repair job will take.")]
		public readonly int Duration = 300;

		[Desc("How many HP does the repair job repair.")]
		public readonly int Amount = 3000;

		[Desc("Cursor used for order.")]
		public readonly string Cursor = "repair";

		[Desc("Cursor used for order if building does not need repair.")]
		public readonly string BlockedCursor = "repair-blocked";

		[Desc("Voice used when ordering to repair.")]
		[VoiceReference] public readonly string VoiceOrder = "Repair";

		[Desc("Voice used when entered and starting repair.")]
		[VoiceReference] public readonly string VoiceEnter = "Repairing";

		public object Create(ActorInitializer init) { return new Technician(init, this); }
	}

	class Technician : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly TechnicianInfo info;

		public Technician(ActorInitializer init, TechnicianInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders { get { yield return new TechnicianEnterOrderTargeter(info.Cursor, info.BlockedCursor); } }

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			return order.OrderID == TechnicianEnterOrderTargeter.Id ? new Order(order.OrderID, self, target, queued) : null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == TechnicianEnterOrderTargeter.Id ? info.VoiceOrder : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != TechnicianEnterOrderTargeter.Id)
				return;

			if (order.Target.Type != TargetType.Actor || order.Target.Actor == null)
				return;

			self.CancelActivity();
			self.QueueActivity(new TechnicianRepair(self, order.Target, info.Amount, info.Duration, info.VoiceEnter, Color.Yellow));
		}
	}
}

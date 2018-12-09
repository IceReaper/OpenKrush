using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Activities;
using OpenRA.Mods.Kknd.Orders;
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

		public IEnumerable<IOrderTargeter> Orders { get { yield return new TechnicianEnterOrderTargeter(info.Cursor); } }

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
			self.SetTargetLine(order.Target, Color.Yellow);
			self.QueueActivity(new TechnicianRepair(self, order.Target.Actor, info.Amount, info.Duration, info.VoiceEnter));
		}
	}
}

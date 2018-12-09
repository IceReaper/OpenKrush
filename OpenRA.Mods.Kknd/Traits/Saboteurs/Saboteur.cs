using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Activities;
using OpenRA.Mods.Kknd.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Saboteurs
{
	[Desc("KKnD specific saboteur implementation.")]
	class SaboteurInfo : ITraitInfo
	{
		[Desc("Cursor used for order.")]
		public readonly string Cursor = "conquer";

		[Desc("Cursor used for order if building does not need to be enforced.")]
		public readonly string BlockedCursor = "conquer-blocked";

		[Desc("Voice used when ordering to enter enemy building.")]
		[VoiceReference] public readonly string VoiceOrderEnemy = "Infiltrate";

		[Desc("Voice used when ordering to enter ally building.")]
		[VoiceReference] public readonly string VoiceOrderAlly = "Reinforce";

		[Desc("Voice used when entered enemy building.")]
		[VoiceReference] public readonly string VoiceEnterEnemy = "Infiltrated";

		[Desc("Voice used when conquered enemy building.")]
		[VoiceReference] public readonly string VoiceConquered = "Conquered";

		[Desc("Voice used when entered ally building.")]
		[VoiceReference] public readonly string VoiceEnterAlly = "Reinforced";

		public object Create(ActorInitializer init) { return new Saboteur(init, this); }
	}

	class Saboteur : IIssueOrder, IResolveOrder, IOrderVoice
	{
		private readonly SaboteurInfo info;

		public Saboteur(ActorInitializer init, SaboteurInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new SaboteurEnterOrderTargeter(info.Cursor, info.BlockedCursor); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			return order.OrderID == SaboteurEnterOrderTargeter.Id ? new Order(order.OrderID, self, target, queued) : null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != SaboteurEnterOrderTargeter.Id)
				return null;

			return order.Target.Actor.Owner.Stances[self.Owner].HasStance(Stance.Ally) ? info.VoiceOrderAlly : info.VoiceOrderEnemy;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != SaboteurEnterOrderTargeter.Id)
				return;

			if (order.Target.Type != TargetType.Actor || order.Target.Actor == null)
				return;

			self.CancelActivity();
			self.SetTargetLine(order.Target, Color.Yellow);
			self.QueueActivity(new SaboteurEnter(self, order.Target.Actor));
		}
	}
}

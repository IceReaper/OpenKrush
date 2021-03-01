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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Mechanics.Saboteurs.Activities;
using OpenRA.Mods.Kknd.Mechanics.Saboteurs.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Saboteurs.Traits
{
	[Desc("KKnD Saboteur mechanism, attach to the unit.")]
	public class SaboteurInfo : TraitInfo
	{
		[Desc("Cursor used for order.")]
		public readonly string Cursor = "conquer";

		[Desc("Cursor used for order if building does not need to be enforced.")]
		public readonly string BlockedCursor = "conquer-blocked";

		[Desc("Target line color.")]
		public readonly Color TargetLineColor = Color.Yellow;

		[Desc("Voice used when ordering to enter enemy building.")]
		[VoiceReference]
		public readonly string VoiceOrderEnemy = "Infiltrate";

		[Desc("Voice used when ordering to enter ally building.")]
		[VoiceReference]
		public readonly string VoiceOrderAlly = "Reinforce";

		[Desc("Voice used when entered enemy building.")]
		[VoiceReference]
		public readonly string VoiceEnterEnemy = "Infiltrated";

		[Desc("Voice used when conquered enemy building.")]
		[VoiceReference]
		public readonly string VoiceConquered = "Conquered";

		[Desc("Voice used when entered ally building.")]
		[VoiceReference]
		public readonly string VoiceEnterAlly = "Reinforced";

		public override object Create(ActorInitializer init)
		{
			return new Saboteur(this);
		}
	}

	public class Saboteur : IIssueOrder, IResolveOrder, IOrderVoice
	{
		private readonly SaboteurInfo info;

		public Saboteur(SaboteurInfo info)
		{
			this.info = info;
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get { yield return new SaboteurEnterOrderTargeter(info.Cursor, info.BlockedCursor); }
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == SaboteurEnterOrderTargeter.Id ? new Order(order.OrderID, self, target, queued) : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != SaboteurEnterOrderTargeter.Id)
				return;

			if (order.Target.Type != TargetType.Actor || order.Target.Actor == null)
				return;

			self.QueueActivity(order.Queued, new SaboteurEnter(self, order.Target, info.TargetLineColor));
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != SaboteurEnterOrderTargeter.Id)
				return null;

			return order.Target.Actor.Owner.RelationshipWith(self.Owner).HasStance(PlayerRelationship.Ally) ? info.VoiceOrderAlly : info.VoiceOrderEnemy;
		}

		public void Enter(Actor self, Actor targetActor)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			if (self.Owner.RelationshipWith(targetActor.Owner).HasStance(PlayerRelationship.Ally))
				self.PlayVoice(info.VoiceEnterAlly);
			else if (targetActor.Trait<SaboteurConquerable>().Population > 0)
				self.PlayVoice(info.VoiceEnterEnemy);
			else
				self.PlayVoice(info.VoiceConquered);
		}
	}
}

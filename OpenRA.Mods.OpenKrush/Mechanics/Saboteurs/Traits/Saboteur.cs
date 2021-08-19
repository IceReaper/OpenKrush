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

namespace OpenRA.Mods.OpenKrush.Mechanics.Saboteurs.Traits
{
	using Activities;
	using Common.Traits;
	using JetBrains.Annotations;
	using OpenRA.Traits;
	using Orders;
	using Primitives;
	using System.Collections.Generic;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[Desc("Saboteur mechanism, attach to the unit.")]
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
			get
			{
				yield return new SaboteurEnterOrderTargeter(this.info.Cursor, this.info.BlockedCursor);
			}
		}

		Order? IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == SaboteurEnterOrderTargeter.Id ? new Order(order.OrderID, self, target, queued) : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != SaboteurEnterOrderTargeter.Id)
				return;

			if (order.Target.Type != TargetType.Actor || order.Target.Actor == null)
				return;

			if (!SaboteurUtils.CanEnter(self, order.Target.Actor))
				return;

			self.QueueActivity(order.Queued, new SaboteurEnter(self, order.Target, this.info.TargetLineColor));
		}

		string? IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != SaboteurEnterOrderTargeter.Id)
				return null;

			return order.Target.Actor.Owner.RelationshipWith(self.Owner).HasRelationship(PlayerRelationship.Ally)
				? this.info.VoiceOrderAlly
				: this.info.VoiceOrderEnemy;
		}

		public void Enter(Actor self, Actor targetActor)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			if (self.Owner.RelationshipWith(targetActor.Owner).HasRelationship(PlayerRelationship.Ally))
				self.PlayVoice(this.info.VoiceEnterAlly);
			else if (targetActor.TraitOrDefault<SaboteurConquerable>().Population > 0)
				self.PlayVoice(this.info.VoiceEnterEnemy);
			else
				self.PlayVoice(this.info.VoiceConquered);
		}
	}
}

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
using OpenRA.Mods.Kknd.Mechanics.Altars.Activities;
using OpenRA.Mods.Kknd.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Altars.Traits
{
	[Desc("Actor can be sacrificed.")]
	class SacrificableInfo : TraitInfo
	{
		[Desc("Cursor used for order.")]
		public readonly string Cursor = "enter";

		[Desc("Target line color.")]
		public readonly Color TargetLineColor = Color.Yellow;

		[Desc("Voice used when ordering to sacrifice.")]
		[VoiceReference]
		public readonly string VoiceOrder = "Action";

		[Desc("Voice used when entered and sacrificed.")]
		[VoiceReference]
		public readonly string VoiceEnter = "Die";

		public override object Create(ActorInitializer init)
		{
			return new Sacrificable(this);
		}
	}

	class Sacrificable : IIssueOrder, IResolveOrder, IOrderVoice
	{
		private readonly SacrificableInfo info;

		public Sacrificable(SacrificableInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new SacrificeOrderTargeter(info.Cursor); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == SacrificeOrderTargeter.Id ? new Order(order.OrderID, self, target, queued) : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != SacrificeOrderTargeter.Id)
				return;

			if (order.Target.Type != TargetType.Actor || order.Target.Actor == null)
				return;

			self.QueueActivity(order.Queued, new Sacrifice(self, order.Target, info.TargetLineColor));
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == SacrificeOrderTargeter.Id ? info.VoiceOrder : null;
		}

		public void Enter(Actor self, Actor targetActor)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			if (self.Owner.RelationshipWith(targetActor.Owner).HasRelationship(PlayerRelationship.Ally))
				self.PlayVoice(info.VoiceEnter);
		}
	}
}

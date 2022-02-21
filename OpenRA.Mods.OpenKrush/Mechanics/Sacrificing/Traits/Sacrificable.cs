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

namespace OpenRA.Mods.OpenKrush.Mechanics.Sacrificing.Traits
{
	using Activities;
	using Common.Traits;
	using JetBrains.Annotations;
	using OpenRA.Traits;
	using Orders;
	using Primitives;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[Desc("Actor can be sacrificed.")]
	public class SacrificableInfo : TraitInfo
	{
		[Desc("Cursor used for order.")]
		public readonly string Cursor = "enter";

		[Desc("Cursor used for order.")]
		public readonly string BlockedCursor = "enter-blocked";

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

	public class Sacrificable : IIssueOrder, IResolveOrder, IOrderVoice
	{
		private readonly SacrificableInfo info;

		public Sacrificable(SacrificableInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new SacrificeOrderTargeter(this.info.Cursor, this.info.BlockedCursor);
			}
		}

		public Order? IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == SacrificeOrderTargeter.Id ? new Order(order.OrderID, self, target, queued) : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != SacrificeOrderTargeter.Id)
				return;

			if (order.Target.Type != TargetType.Actor || order.Target.Actor == null)
				return;

			if (!SacrificingUtils.CanEnter(self, order.Target.Actor, out _))
				return;

			self.QueueActivity(order.Queued, new Sacrifice(self, order.Target, this.info.TargetLineColor));
		}

		string? IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == SacrificeOrderTargeter.Id && SacrificingUtils.CanEnter(self, order.Target.Actor, out _) ? this.info.VoiceOrder : null;
		}

		public void Enter(Actor self)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			self.PlayVoice(this.info.VoiceEnter);
		}
	}
}

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
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Kknd.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Behavior
{
	[Desc("Deploy when standing on top of a specific actor.")]
	class DeploysOnActorInfo : ITraitInfo
	{
		[Desc("Actor to transform into."), ActorReference, FieldLoader.Require]
		public readonly string IntoActor = null;

		[Desc("Cursor to display when hovering an oilpatch.")]
		public readonly string DeployCursor = null;

		[Desc("Actors which this actor can deploy on.")]
		public readonly string[] ValidTargets = { };

		public readonly CVec Offset = CVec.Zero;

		public object Create(ActorInitializer init) { return new DeploysOnActor(init, this); }
	}

	class DeploysOnActor : IIssueOrder, ITick
	{
		private readonly DeploysOnActorInfo info;
		private bool issued;

		public DeploysOnActor(ActorInitializer init, DeploysOnActorInfo info)
		{
			this.info = info;
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders { get { yield return new DeployOnActorOrderTargeter(info.ValidTargets, info.DeployCursor); } }

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			return order is DeployOnActorOrderTargeter ? new Order("Move", self, Target.FromCell(self.World, self.World.Map.CellContaining(target.CenterPosition)), queued) : null;
		}

		void ITick.Tick(Actor self)
		{
			if (issued || !self.IsIdle)
				return;

			var actors = self.World.FindActorsOnCircle(self.CenterPosition, new WDist(512)).Where(actor =>
			{
				if (actor == self)
					return false;

				if (!info.ValidTargets.Contains(actor.Info.Name))
					return false;

				return actor.CenterPosition - self.CenterPosition == WVec.Zero;
			});

			if (!actors.Any())
				return;

			issued = true;

			self.QueueActivity(new Transform(self, info.IntoActor)
			{
				Faction = self.Owner.Faction.InternalName,
				Offset = info.Offset
			});
		}
	}
}

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

namespace OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits;

using Activities;
using Common;
using Common.Activities;
using JetBrains.Annotations;
using OpenRA.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("Deploy when standing on top of a specific actor.")]
public class DeploysOnActorInfo : TraitInfo
{
	[Desc("Actor to transform into.")]
	[ActorReference]
	[FieldLoader.RequireAttribute]
	public readonly string? IntoActor;

	[Desc("Cursor to display when hovering target actor.")]
	public readonly string DeployCursor = "deploy";

	[Desc("Actors which this actor can deploy on.")]
	public readonly string[] ValidTargets = Array.Empty<string>();

	public readonly CVec Offset = CVec.Zero;

	public override object Create(ActorInitializer init)
	{
		return new DeploysOnActor(this);
	}
}

public class DeploysOnActor : IIssueOrder, ITick
{
	private readonly DeploysOnActorInfo info;
	private bool issued;

	public DeploysOnActor(DeploysOnActorInfo info)
	{
		this.info = info;
	}

	IEnumerable<IOrderTargeter> IIssueOrder.Orders
	{
		get
		{
			yield return new DeployOnActorOrderTargeter(this.info.ValidTargets, this.info.DeployCursor);
		}
	}

	public Order? IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
	{
		return order is DeployOnActorOrderTargeter
			? new Order("Move", self, Target.FromCell(self.World, self.World.Map.CellContaining(target.CenterPosition)), queued)
			: null;
	}

	void ITick.Tick(Actor self)
	{
		if (this.issued || !self.IsIdle)
			return;

		var actors = self.World.FindActorsOnCircle(self.CenterPosition, new(512))
			.Where(
				actor =>
				{
					if (actor.Equals(self))
						return false;

					if (!this.info.ValidTargets.Contains(actor.Info.Name))
						return false;

					return actor.CenterPosition - self.CenterPosition == WVec.Zero;
				}
			);

		if (!actors.Any())
			return;

		this.issued = true;

		self.QueueActivity(new Transform(this.info.IntoActor) { Faction = self.Owner.Faction.InternalName, Offset = this.info.Offset });
	}
}

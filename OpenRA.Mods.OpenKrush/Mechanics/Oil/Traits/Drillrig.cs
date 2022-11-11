#region Copyright & License Information

/*
 * Copyright 2007-2022 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits;

using Docking.Traits;
using JetBrains.Annotations;
using OpenRA.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("Drillrig logic.")]
public class DrillrigInfo : DockActionInfo
{
	[GrantedConditionReference]
	[Desc("Condition, which will be granted if the Drillrig is not empty.")]
	public readonly string Condition = "HasOil";

	[Desc("Notification to play when oil is low.")]
	public readonly string LowNotification = "DrillrigLow";

	[Desc("Notification to play when oil is depleted.")]
	public readonly string EmptyNotification = "DrillrigEmpty";

	[Desc("How many oil per tick should be pumped.")]
	public readonly int Rate = 3;

	public override object Create(ActorInitializer init)
	{
		return new Drillrig(this);
	}
}

public class Drillrig : DockAction, ITick, IHaveOil, INotifyRemovedFromWorld
{
	private readonly DrillrigInfo info;

	private Actor? oilPatchActor;
	private OilPatch? oilPatch;

	private int token = Actor.InvalidConditionToken;

	public int Current => this.oilPatch?.Current ?? 0;
	public int Maximum => this.oilPatch?.Info.FullAmount ?? 1;

	public Drillrig(DrillrigInfo info)
		: base(info)
	{
		this.info = info;
	}

	protected override void Created(Actor self)
	{
		base.Created(self);

		var actor = self.World.FindActorsInCircle(self.CenterPosition, new(1024)).FirstOrDefault(a => a.Info.HasTraitInfo<OilPatchInfo>());

		if (actor == null)
			return;

		this.oilPatchActor = actor;
		this.oilPatch = this.oilPatchActor.TraitOrDefault<OilPatch>();
		this.oilPatch.Drillrig = self;

		this.token = self.GrantCondition(this.info.Condition);
	}

	public override bool CanDock(Actor self, Actor target)
	{
		return target.Info.HasTraitInfo<TankerInfo>() && this.oilPatch != null;
	}

	public override bool Process(Actor self, Actor actor)
	{
		if (this.oilPatch == null)
			return true;

		var tanker = actor.TraitOrDefault<Tanker>();
		var amount = this.oilPatch.Pull(this.info.Rate);
		var remaining = tanker.Push(amount);

		this.oilPatch.Push(remaining);

		return this.oilPatch == null || tanker.Current == tanker.Maximum;
	}

	public override void OnDock(Actor self)
	{
		if (this.oilPatch is { Current: <= 2500 })
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", this.info.LowNotification, self.Owner.Faction.InternalName);
	}

	void ITick.Tick(Actor self)
	{
		if (this.oilPatchActor is not { IsDead: true })
			return;

		Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", this.info.EmptyNotification, self.Owner.Faction.InternalName);

		this.oilPatchActor = null;
		this.oilPatch = null;
		
		if (this.token != Actor.InvalidConditionToken)
			this.token = self.RevokeCondition(this.token);
	}

	void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
	{
		if (this.oilPatch != null)
			this.oilPatch.Drillrig = null;
	}
}

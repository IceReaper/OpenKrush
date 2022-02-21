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

namespace OpenRA.Mods.OpenKrush.Mechanics.Misc.Traits;

using Common.Traits;
using Common.Traits.Render;
using JetBrains.Annotations;
using OpenRA.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class WithAimAttackAnimationInfo : TraitInfo, Requires<WithSpriteBodyInfo>
{
	[Desc("Displayed while attacking.")]
	[SequenceReference]
	public readonly string SequenceFire = "";

	[Desc("Displayed while attacking.")]
	[SequenceReference]
	public readonly string SequenceAim = "";

	public override object Create(ActorInitializer init)
	{
		return new WithAimAttackAnimation(init, this);
	}
}

public class WithAimAttackAnimation : ITick, INotifyAttack, INotifyAiming
{
	private readonly WithAimAttackAnimationInfo info;
	private readonly WithSpriteBody wsb;
	private bool aiming;

	public WithAimAttackAnimation(ActorInitializer init, WithAimAttackAnimationInfo info)
	{
		this.info = info;
		this.wsb = init.Self.TraitOrDefault<WithSpriteBody>();
	}

	void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
	{
		this.wsb.PlayCustomAnimation(self, this.info.SequenceFire);
	}

	void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel)
	{
	}

	void ITick.Tick(Actor self)
	{
		if (!string.IsNullOrEmpty(this.info.SequenceAim) && this.aiming && this.wsb.DefaultAnimation.CurrentSequence.Name == "idle")
			this.wsb.PlayCustomAnimation(self, this.info.SequenceAim);
	}

	void INotifyAiming.StartedAiming(Actor self, AttackBase attack)
	{
		this.aiming = true;
	}

	void INotifyAiming.StoppedAiming(Actor self, AttackBase attack)
	{
		this.aiming = false;
	}
}

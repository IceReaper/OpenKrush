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
using JetBrains.Annotations;
using OpenRA.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("Play together with an attack.")]
public class WithLoopedAttackSoundInfo : TraitInfo
{
	[FieldLoader.RequireAttribute]
	[Desc("Sound filename to use")]
	public readonly string[] Report = Array.Empty<string>();

	public readonly int Delay = 10;

	public override object Create(ActorInitializer init)
	{
		return new WithLoopedAttackSound(this);
	}
}

public class WithLoopedAttackSound : INotifyAttack, ITick, INotifyRemovedFromWorld
{
	private readonly WithLoopedAttackSoundInfo info;

	private ISound? sound;
	private int tick;

	public WithLoopedAttackSound(WithLoopedAttackSoundInfo info)
	{
		this.info = info;
	}

	void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
	{
		this.sound ??= Game.Sound.PlayLooped(SoundType.World, this.info.Report.Random(self.World.SharedRandom), self.CenterPosition);
		this.tick = self.World.WorldTick + this.info.Delay;
	}

	void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel)
	{
	}

	void ITick.Tick(Actor self)
	{
		if (this.sound == null || this.tick >= self.World.WorldTick)
			return;

		Game.Sound.StopSound(this.sound);
		this.sound = null;
	}

	void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
	{
		if (this.sound != null)
			Game.Sound.StopSound(this.sound);
	}
}

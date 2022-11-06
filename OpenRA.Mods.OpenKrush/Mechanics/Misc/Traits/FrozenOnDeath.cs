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

namespace OpenRA.Mods.OpenKrush.Mechanics.Misc.Traits;

using Common.Traits;
using JetBrains.Annotations;
using OpenRA.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("This actor will be visible for a particular time when being killed.")]
public class FrozenOnDeathInfo : TraitInfo, Requires<HealthInfo>
{
	[Desc("The amount of ticks the death state will be visible.")]
	public readonly int Duration = 25;

	public override object Create(ActorInitializer init)
	{
		return new FrozenOnDeath(init, this);
	}
}

public class FrozenOnDeath : ITick
{
	private int despawn;

	public FrozenOnDeath(ActorInitializer init, FrozenOnDeathInfo info)
	{
		this.despawn = info.Duration;
		init.Self.TraitOrDefault<Health>().RemoveOnDeath = false;
	}

	void ITick.Tick(Actor self)
	{
		if (!self.IsDead)
			return;

		if (--this.despawn <= 0)
			self.Dispose();
	}
}

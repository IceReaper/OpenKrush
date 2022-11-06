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

namespace OpenRA.Mods.OpenKrush.Mechanics.AI.Traits;

using Common.Traits;
using JetBrains.Annotations;
using Oil.Traits;
using OpenRA.Traits;
using Researching.Traits;

[UsedImplicitly]
public class BotFakeResourcesInfo : ConditionalTraitInfo
{
	public override object Create(ActorInitializer init)
	{
		return new BotFakeResources(this);
	}
}

public class BotFakeResources : ConditionalTrait<BotFakeResourcesInfo>, ITick
{
	public BotFakeResources(BotFakeResourcesInfo info)
		: base(info)
	{
	}

	void ITick.Tick(Actor self)
	{
		if (!self.Owner.IsBot)
			return;

		if (self.Owner.World.WorldTick % 3 != 0)
			return;

		var pumpForce = self.Owner.World.ActorsHavingTrait<PowerStation>()
			.Where(a => a.Owner == self.Owner)
			.Sum(a => 5 + a.TraitOrDefault<Researchable>().Level);

		self.Owner.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(Math.Max(5, pumpForce));
	}
}

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
using OpenRA.Traits;
using Researching;
using Researching.Orders;
using Researching.Traits;

[UsedImplicitly]
public class BotBaseResearcherInfo : ConditionalTraitInfo
{
	public override object Create(ActorInitializer init)
	{
		return new BotBaseResearcher(this);
	}
}

public class BotBaseResearcher : ConditionalTrait<BotBaseResearcherInfo>, IBotTick
{
	public BotBaseResearcher(BotBaseResearcherInfo info)
		: base(info)
	{
	}

	void IBotTick.BotTick(IBot bot)
	{
		// For performance we delay some ai tasks => OpenKrush runs with 25 ticks per second (at normal speed).
		if (bot.Player.World.WorldTick % 25 != 0)
			return;

		var researcher = bot.Player.World.Actors.FirstOrDefault(
			a =>
			{
				if (a.Owner != bot.Player)
					return false;

				var researches = a.TraitOrDefault<Researches>();

				return researches is { IsTraitDisabled: false } && researches.GetState() == ResarchState.Available;
			}
		);

		if (researcher == null)
			return;

		var researchables = bot.Player.World.ActorsWithTrait<Researchable>()
			.Where(e => e.Actor.Owner == bot.Player && !e.Trait.IsTraitDisabled && e.Trait.Level < e.Trait.MaxLevel && e.Trait.ResearchedBy == null)
			.OrderBy(e => e.Trait.Level)
			.Select(e => e.Actor)
			.ToArray();

		if (researchables.Length == 0)
			return;

		((IResolveOrder)researcher.TraitOrDefault<Researches>()).ResolveOrder(
			researcher,
			new(ResearchOrderTargeter.Id, researcher, Target.FromActor(researchables[0]), false)
		);
	}
}

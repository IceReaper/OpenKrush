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

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Orders;
using OpenRA.Mods.Kknd.Traits.Research;
using OpenRA.Mods.Kknd.Traits.Resources;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.AI
{
	// TODO replace this completely when AI is modular!
	[Desc("Ugly ugly hack to give the ai cash and make it research random buildings.")]
	public class HackedAiShitInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new HackedAiShit(); }
	}

	public class HackedAiShit : ITick
	{
		void ITick.Tick(Actor self)
		{
			if (!self.Owner.IsBot)
				return;

			if (self.World.WorldTick % 3 == 0)
			{
				var pumpForce = self.World.ActorsHavingTrait<PowerStation>().Where(a => a.Owner == self.Owner).Sum(a => 5 + a.Trait<Researchable>().Level);
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(Math.Max(5, pumpForce));
			}

			var researcher = self.World.Actors.FirstOrDefault(a =>
			{
				if (a.Owner != self.Owner)
					return false;

				var researches = a.TraitOrDefault<Researches>();

				return researches != null && !researches.IsTraitDisabled && !researches.IsResearching;
			});

			if (researcher == null)
				return;

			var researchables = self.World.Actors.Where(a =>
			{
				if (a.Owner != self.Owner)
					return false;

				if (a.TraitOrDefault<PowerStation>() != null && self.World.SharedRandom.Next(0, 10) != 0)
					return false;

				var researchable = a.TraitOrDefault<Researchable>();

				return researchable != null && !researchable.IsTraitDisabled && researchable.Level < researchable.Info.MaxLevel && researchable.Researches == null;
			}).ToArray();

			if (researchables.Length == 0)
				return;

			var target = researchables[self.World.SharedRandom.Next(0, researchables.Length)];
			((IResolveOrder)researcher.Trait<Researches>()).ResolveOrder(researcher, new Order(ResearchOrderTargeter.Id, researcher, Target.FromActor(target), false));
		}
	}
}

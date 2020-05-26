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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Kknd.Traits.Research;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Orders
{
	public class ResearchOrderGenerator : OrderGenerator
	{
		private IEnumerable<TraitPair<Researches>> researchActors;

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
				world.CancelInputMode();
			else if (researchActors != null)
			{
				var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(target =>
				{
					if (!target.Info.HasTraitInfo<ResearchableInfo>())
						return false;

					return researchActors.Any(researchActor => researchActor.Trait.IsValidTarget(researchActor.Actor, target));
				});

				if (actor == null)
					yield break;

				var researches = researchActors.OrderBy(e => e.Trait.IsResearching ? 1 : 0).First().Actor;
				yield return new Order(ResearchOrderTargeter.Id, researches, Target.FromActor(actor), false);
			}
		}

		protected override void Tick(World world)
		{
			researchActors = world.ActorsWithTrait<Researches>().Where(e => e.Actor.Owner == world.LocalPlayer && !e.Trait.IsTraitDisabled);

			if (!researchActors.Any())
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (researchActors == null)
				return null;

			var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(target =>
			{
				if (!target.Info.HasTraitInfo<ResearchableInfo>())
					return false;

				return researchActors.Any(researchActor => researchActor.Trait.IsValidTarget(researchActor.Actor, target));
			});

			if (actor == null)
				return null;

			return actor.Trait<Researchable>().Researches == null ? "research" : "research-blocked";
		}
	}

	public class ResearchOrderTargeter : UnitOrderTargeter
	{
		public const string Id = "Research";

		private readonly string cursor;
		private readonly string blockedCursor;

		public ResearchOrderTargeter(string cursor, string blockedCursor)
			: base(Id, 6, cursor, false, true)
		{
			this.cursor = cursor;
			this.blockedCursor = blockedCursor;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if (!self.Trait<Researches>().IsValidTarget(self, target))
			{
				cursor = null;
				return false;
			}

			cursor = target.TraitOrDefault<Researchable>().Researches != null ? blockedCursor : this.cursor;
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}
	}
}

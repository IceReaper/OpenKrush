using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Kknd.Traits.Research;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Orders
{
	public class ResearchOrderGenerator : IOrderGenerator
	{
		private IEnumerable<TraitPair<Researches>> researchActors;

		public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
				world.CancelInputMode();
			else
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

		public virtual void Tick(World world)
		{
			researchActors = world.ActorsWithTrait<Researches>().Where(e => e.Actor.Owner == world.LocalPlayer && !e.Trait.IsTraitDisabled);

			if (!researchActors.Any())
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
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

		private string cursor;
		private readonly string blockedCursor;

		public ResearchOrderTargeter(string cursor, string blockedCursor) : base(Id, 6, cursor, false, true)
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

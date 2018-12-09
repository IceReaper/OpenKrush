using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Kknd.Traits.Technicians;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Orders
{
	public class TechnicianEnterOrderGenerator : IOrderGenerator
	{
		private IEnumerable<TraitPair<Technician>> technicians;

		public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
				world.CancelInputMode();
			else
			{
				var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a =>
				{
					if (a.Owner != world.LocalPlayer)
						return false;

					var technicianRepairable = a.TraitOrDefault<TechnicianRepairable>();
					return technicianRepairable != null && !technicianRepairable.IsTraitDisabled;
				});

				if (actor != null)
				{
					var researches = technicians.OrderBy(e => (e.Actor.CenterPosition - actor.CenterPosition).Length).First().Actor;
					yield return new Order(TechnicianEnterOrderTargeter.Id, researches, Target.FromActor(actor), false);
				}
			}
		}

		public virtual void Tick(World world)
		{
			technicians = world.ActorsWithTrait<Technician>().Where(e => e.Actor.Owner == world.LocalPlayer && e.Actor.IsIdle);

			if (!technicians.Any())
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a =>
			{
				if (a.Owner != world.LocalPlayer)
					return false;

				var technicianRepairable = a.TraitOrDefault<TechnicianRepairable>();
				return technicianRepairable != null && !technicianRepairable.IsTraitDisabled;
			});
			return actor != null ? "repair" : null;
		}
	}

	class TechnicianEnterOrderTargeter : UnitOrderTargeter
	{
		public const string Id = "TechnicianEnter";

		private string cursor;

		public TechnicianEnterOrderTargeter(string cursor) : base(Id, 6, cursor, false, true)
		{
			this.cursor = cursor;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			cursor = null;

			if (!target.Info.HasTraitInfo<TechnicianRepairableInfo>())
				return false;

			if (target.Trait<TechnicianRepairable>().IsTraitDisabled)
				return false;

			if (self.Owner.Stances[target.Owner] != Stance.Ally)
				return false;

			cursor = this.cursor;
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}
	}
}

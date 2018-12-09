using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Kknd.Traits.Production;

namespace OpenRA.Mods.Kknd.Orders
{
	public class SellOrderGenerator : IOrderGenerator
	{
		public const string Id = "Sell";

		private IEnumerable<TraitPair<DeconstructSellable>> sellableActors;

		public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
				world.CancelInputMode();
			else
			{
				var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a => sellableActors.Any(e => e.Actor == a));
				if (actor != null)
					yield return new Order(Id, actor, false);
			}
		}

		public virtual void Tick(World world)
		{
			sellableActors = world.ActorsWithTrait<DeconstructSellable>().Where(e => e.Actor.Owner == world.LocalPlayer && !e.Trait.IsTraitDisabled);

			if (!sellableActors.Any())
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (sellableActors == null)
				return null;

			var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a => sellableActors.Any(e => e.Actor == a));

			if (actor != null)
				return "sell";

			return null;
		}
	}
}

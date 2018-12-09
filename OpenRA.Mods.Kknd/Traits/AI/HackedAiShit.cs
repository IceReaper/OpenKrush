using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Orders;
using OpenRA.Mods.Kknd.Traits.Research;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.AI
{
    // TODO replace this completely when AI is modular!
    [Desc("Ugly ugly hack to give the ai cash and make it research random buildings.")]
    public class HackedAiShitInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new HackedAiShit(init.Self); }
    }

    public class HackedAiShit : ITick
    {
        private bool isActive;

        public HackedAiShit(Actor self)
        {
            isActive = self.Owner.IsBot;
        }

        void ITick.Tick(Actor self)
        {
            if (!isActive)
                return;

            var pr = self.Owner.PlayerActor.Trait<PlayerResources>();
            if (pr.Cash < 5000)
                pr.GiveCash(5000);

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

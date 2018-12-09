using System;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Kknd.Traits.Production
{
    public class AdvancedProductionInfo : ProductionInfo
    {   
        public readonly int MaximumDistance = 3;
        
        public override object Create(ActorInitializer init) { return new AdvancedProduction(init, this); }
    }
    
    public class AdvancedProduction : Common.Traits.Production
    {
        private AdvancedProductionInfo info;

        public AdvancedProduction(ActorInitializer init, AdvancedProductionInfo info) : base(init, info)
        {
            this.info = info;
        }

        protected override Exit SelectExit(Actor self, ActorInfo producee, string productionType, Func<Exit, bool> p)
        {
            var mobileInfo = producee.TraitInfoOrDefault<MobileInfo>();

            var exit = base.SelectExit(self, producee, productionType, null);
            var spawn = self.World.Map.CellContaining(self.CenterPosition + exit.Info.SpawnOffset);

            for (var y = 1; y <= info.MaximumDistance; y++)
            for (var x = -y; x <= y; x++)
            {
                var candidate = new CVec(x, y);

                if (!mobileInfo.CanEnterCell(self.World, self, spawn + candidate, self))
                    continue;

                var exitInfo = new ExitInfo();
                exitInfo.GetType().GetField("SpawnOffset").SetValue(exitInfo, exit.Info.SpawnOffset);
                exitInfo.GetType().GetField("ExitCell").SetValue(exitInfo, spawn - self.Location + candidate);
                exitInfo.GetType().GetField("Facing").SetValue(exitInfo, exit.Info.Facing);
                
                return new Exit(null, exitInfo);
            }
            
            return null;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Traits.Research;

namespace OpenRA.Mods.Kknd.Traits.Production
{
	[Desc("This version of ParallelProductionQueue references prerequisites and techlevel to itself.")]
	public class AdvancedProductionQueueInfo : ParallelProductionQueueInfo
	{
		public override object Create(ActorInitializer init) { return new AdvancedProductionQueue(init, init.Self.Owner.PlayerActor, this); }
	}

	public class AdvancedProductionQueue : ParallelProductionQueue
	{
		private readonly Actor actor;

		public AdvancedProductionQueue(ActorInitializer init, Actor playerActor, AdvancedProductionQueueInfo info) : base(init, playerActor, info)
		{
			actor = init.Self;
		}

		public override IEnumerable<ActorInfo> BuildableItems()
		{
			if (productionTraits.Any() && productionTraits.All(p => p.IsTraitDisabled))
				return Enumerable.Empty<ActorInfo>();
			if (!Enabled)
				return Enumerable.Empty<ActorInfo>();
			if (developerMode.AllTech)
				return Producible.Keys;

			return Producible.Keys.Where(prod =>
			{
				var buildable = prod.TraitInfoOrDefault<BuildableInfo>();

				if (buildable == null)
					return true;

				if (buildable.BuildLimit > 0 && buildable.BuildLimit <= Actor.World.ActorsHavingTrait<Buildable>().Count(a => a.Info.Name == prod.Name && a.Owner == Actor.Owner))
					return false;

				return ProducerHasRequirements(buildable);
			});
		}

		protected virtual bool ProducerHasRequirements(BuildableInfo buildable)
		{
			if (!Actor.Info.TraitInfos<ProvidesPrerequisiteInfo>().Any(providesPrerequisite => buildable.Prerequisites.Contains(providesPrerequisite.Prerequisite)))
				return false;

			var advancedBuildable = buildable as AdvancedBuildableInfo;

			if (advancedBuildable == null)
				return true;

			if (advancedBuildable.Level == -1)
				return false;

			var researchable = actor.TraitOrDefault<Researchable>();
			
			return advancedBuildable.Level == 0 || (researchable != null && advancedBuildable.Level <= researchable.Level);
		}
	}
}

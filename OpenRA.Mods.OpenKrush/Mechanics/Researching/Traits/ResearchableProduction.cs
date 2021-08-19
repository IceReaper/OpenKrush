#region Copyright & License Information

/*
 * Copyright 2007-2021 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Mechanics.Researching.Traits
{
	using Common.Traits;
	using System.Collections.Generic;
	using System.Linq;

	public class ResearchableProductionInfo : ProductionInfo
	{
		public override object Create(ActorInitializer init)
		{
			return new ResearchableProduction(init, this);
		}
	}

	public class ResearchableProduction : Production, IProvidesResearchables
	{
		public ResearchableProduction(ActorInitializer init, ProductionInfo info)
			: base(init, info)
		{
		}

		public Dictionary<string, int> GetResearchables(Actor self)
		{
			var providesPrerequisite = self.TraitsImplementing<ProvidesPrerequisite>();

			return self.World.Map.Rules.Actors.Values.Where(
					actorInfo =>
					{
						if (actorInfo.Name[0] == '^')
							return false;

						var buildableInfo = actorInfo.TraitInfoOrDefault<BuildableInfo>();

						if (buildableInfo == null)
							return false;

						return !buildableInfo.Prerequisites.Any(p => providesPrerequisite.All(pp => pp.Info.Prerequisite != p))
							&& buildableInfo.Queue.Any(q => this.Info.Produces.Contains(q));
					}
				)
				.ToDictionary(
					actorInfo => TechLevelBuildableInfo.Prefix + actorInfo.Name,
					actorInfo => actorInfo.TraitInfoOrDefault<TechLevelBuildableInfo>()?.Level ?? 0
				);
		}
	}
}

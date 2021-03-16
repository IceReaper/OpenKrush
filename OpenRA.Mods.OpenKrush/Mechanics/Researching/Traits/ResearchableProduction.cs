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
	using System.Collections.Generic;
	using System.Linq;
	using Common.Traits;

	public class ResearchableProductionInfo : ProductionInfo
	{
		public override object Create(ActorInitializer init)
		{
			return new ResearchableProduction(init, this);
		}
	}

	public class ResearchableProduction : Production, IProvidesResearchables
	{
		private readonly Actor self;

		public ResearchableProduction(ActorInitializer init, ProductionInfo info)
			: base(init, info)
		{
			self = init.Self;
		}

		public Dictionary<string, int> GetResearchables()
		{
			var result = new Dictionary<string, int>();
			var providesPrerequisite = self.TraitsImplementing<ProvidesPrerequisite>();

			foreach (var actorInfo in self.World.Map.Rules.Actors.Values.Where(
				x =>
				{
					if (x.Name[0] == '^')
						return false;

					var buildableInfo = x.TraitInfoOrDefault<BuildableInfo>();

					if (buildableInfo == null)
						return false;

					if (buildableInfo.Prerequisites.Any(p => providesPrerequisite.All(pp => pp.Info.Prerequisite != p)))
						return false;

					return buildableInfo.Queue.Any(q => Info.Produces.Contains(q));
				}))
			{
				result.Add(TechLevelBuildableInfo.Prefix + actorInfo.Name, actorInfo.TraitInfoOrDefault<TechLevelBuildableInfo>()?.Level ?? 0);
			}

			return result;
		}
	}
}

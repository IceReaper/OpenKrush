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

namespace OpenRA.Mods.OpenKrush.Mechanics.Production.Traits
{
	using Common.Traits;
	using JetBrains.Annotations;
	using Researching.Traits;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class AdvancedProductionInfo : ResearchableProductionInfo
	{
		public readonly int MaximumDistance = 3;

		public override object Create(ActorInitializer init)
		{
			return new AdvancedProduction(init, this);
		}
	}

	public class AdvancedProduction : ResearchableProduction
	{
		private readonly AdvancedProductionInfo info;

		public AdvancedProduction(ActorInitializer init, AdvancedProductionInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		protected override Exit? SelectExit(Actor self, ActorInfo producee, string productionType, Func<Exit, bool> p)
		{
			var mobileInfo = producee.TraitInfoOrDefault<MobileInfo>();

			var exit = base.SelectExit(self, producee, productionType, null);
			var spawn = self.World.Map.CellContaining(self.CenterPosition + exit.Info.SpawnOffset);

			for (var y = 1; y <= this.info.MaximumDistance; y++)
			for (var x = -y; x <= y; x++)
			{
				var candidate = new CVec(x, y);

				if (!mobileInfo.CanEnterCell(self.World, self, spawn + candidate))
					continue;

				var exitInfo = new ExitInfo();
				exitInfo.GetType().GetField("SpawnOffset")?.SetValue(exitInfo, exit.Info.SpawnOffset);
				exitInfo.GetType().GetField("ExitCell")?.SetValue(exitInfo, spawn - self.Location + candidate);
				exitInfo.GetType().GetField("Facing")?.SetValue(exitInfo, exit.Info.Facing);

				return new(exitInfo);
			}

			return null;
		}
	}
}

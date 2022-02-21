#region Copyright & License Information

/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Mechanics.AI.Traits;

using Common.Traits;
using JetBrains.Annotations;
using Oil.Traits;
using Ui.Traits;

[UsedImplicitly]
public class BotProtectionGeneratorInfo : ConditionalTraitInfo
{
	public override object Create(ActorInitializer init)
	{
		return new BotProtectionGenerator(this);
	}
}

public class BotProtectionGenerator : ConditionalTrait<BotProtectionGeneratorInfo>
{
	public BotProtectionGenerator(BotProtectionGeneratorInfo info)
		: base(info)
	{
	}

	protected override void Created(Actor self)
	{
		var info = self.TraitOrDefault<SquadManagerBotModule>()?.Info;

		info?.GetType()
			.GetField(nameof(SquadManagerBotModuleInfo.ProtectionTypes))
			?.SetValue(
				info,
				self.World.Map.Rules.Actors
					.Where(
						a => (a.Value.HasTraitInfo<BuildingInfo>() && a.Value.HasTraitInfo<AdvancedSelectionDecorationsInfo>())
							|| a.Value.HasTraitInfo<TankerInfo>()
					)
					.Select(e => e.Key)
					.ToHashSet()
			);
	}
}

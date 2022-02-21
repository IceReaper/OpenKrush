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

namespace OpenRA.Mods.OpenKrush.Mechanics.Researching;

using Traits;

public static class ResearchUtils
{
	public const string LobbyOptionsCategory = "research";

	public static ResearchAction GetAction(Actor self, Actor target)
	{
		if (target.IsDead || target.Disposed || !target.IsInWorld)
			return ResearchAction.None;

		if (target.Owner != self.Owner)
			return ResearchAction.None;

		var researches = self.TraitOrDefault<Researches>();
		var researchable = target.TraitOrDefault<Researchable>();

		if (researches == null || researches.IsTraitDisabled || researchable == null)
			return ResearchAction.None;

		if (researchable.IsTraitDisabled)
			return ResearchAction.Blocked;

		if (researchable.ResearchedBy != null)
			return ResearchAction.Stop;

		if (researches.GetState() == ResarchState.Researching)
			return ResearchAction.Blocked;

		return researchable.Level >= researchable.MaxLevel ? ResearchAction.Blocked : ResearchAction.Start;
	}
}

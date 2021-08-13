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

namespace OpenRA.Mods.OpenKrush.Mechanics.Researching
{
	using Traits;

	public static class ResearchUtils
	{
		public const string LobbyOptionsCategory = "research";

		public static ResearchAction GetAction(Actor self, Actor target)
		{
			if (target.Disposed)
				return ResearchAction.None;

			if (target.Owner != self.Owner)
				return ResearchAction.None;

			var researches = self.Trait<Researches>();

			if (researches == null)
				return ResearchAction.None;

			var researchesState = researches.GetState();

			if (researchesState == ResarchState.Unavailable)
				return ResearchAction.None;

			var researchable = target.TraitOrDefault<Researchable>();

			if (researchable == null)
				return ResearchAction.None;

			var currentState = researchable.GetState();

			if (currentState == ResarchState.Unavailable)
				return ResearchAction.None;

			if (currentState == ResarchState.Researching)
				return ResearchAction.Stop;

			if (researchesState == ResarchState.Researching)
				return ResearchAction.None;

			if (researchable.Level >= researchable.MaxLevel)
				return ResearchAction.None;

			return ResearchAction.Start;
		}
	}
}

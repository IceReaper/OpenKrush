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

			var researches = self.TraitOrDefault<Researches>();

			if (researches == null)
				return ResearchAction.None;

			var researchesState = researches.GetState();

			if (researchesState == ResarchState.Unavailable)
				return ResearchAction.None;

			var researchable = target.TraitOrDefault<Researchable>();

			if (researchable == null)
				return ResearchAction.None;

			var currentState = researchable.GetState(target);

			switch (currentState)
			{
				case ResarchState.Unavailable:
					return ResearchAction.None;

				case ResarchState.Researching:
					return ResearchAction.Stop;

				case ResarchState.Available:
					break;

				default:
					throw new ArgumentOutOfRangeException(Enum.GetName(currentState));
			}

			if (researchesState == ResarchState.Researching)
				return ResearchAction.None;

			return researchable.Level >= researchable.MaxLevel ? ResearchAction.None : ResearchAction.Start;
		}
	}
}

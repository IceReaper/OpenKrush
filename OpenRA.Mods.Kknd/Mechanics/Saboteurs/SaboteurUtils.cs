#region Copyright & License Information

/*
 * Copyright 2016-2020 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

using OpenRA.Mods.Kknd.Mechanics.Saboteurs.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Saboteurs
{
	public static class SaboteurUtils
	{
		public static bool CanEnter(Actor saboteur, Actor target)
		{
			var saboteurConquerable = target.TraitOrDefault<SaboteurConquerable>();

			if (saboteurConquerable == null || saboteurConquerable.IsTraitDisabled)
				return false;

			return saboteur.Owner.Stances[target.Owner] != Stance.Ally || saboteurConquerable.Population != saboteurConquerable.Info.MaxPopulation;
		}
	}
}

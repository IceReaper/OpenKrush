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

using OpenRA.Mods.OpenKrush.Mechanics.Saboteurs.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenKrush.Mechanics.Saboteurs
{
	public static class SaboteurUtils
	{
		public static bool CanEnter(Actor source, Actor target)
		{
			var trait = target.TraitOrDefault<SaboteurConquerable>();

			if (trait == null || trait.IsTraitDisabled)
				return false;

			return source.Owner.RelationshipWith(target.Owner) != PlayerRelationship.Ally || trait.Population != trait.Info.MaxPopulation;
		}
	}
}

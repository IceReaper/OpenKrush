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

namespace OpenRA.Mods.OpenKrush.Mechanics.Technicians
{
	using Bunkers.Traits;
	using OpenRA.Traits;
	using Traits;

	public static class TechnicianUtils
	{
		public static bool CanEnter(Actor source, Actor target)
		{
			var bunker = target.TraitOrDefault<TechBunker>();

			if (bunker != null)
				return bunker.State == TechBunkerState.ClosedUnlocked;

			var trait = target.TraitOrDefault<TechnicianRepairable>();

			if (trait == null || trait.IsTraitDisabled)
				return false;

			return source.Owner.RelationshipWith(target.Owner) == PlayerRelationship.Ally;
		}
	}
}

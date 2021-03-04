#region Copyright & License Information
/*
 * Copyright 2007-2021 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Kknd.Mechanics.Technicians.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Technicians
{
	public static class TechnicianUtils
	{
		public static bool CanEnter(Actor source, Actor target)
		{
			var trait = target.TraitOrDefault<TechnicianRepairable>();

			if (trait == null || trait.IsTraitDisabled)
				return false;

			return source.Owner.RelationshipWith(target.Owner) == PlayerRelationship.Ally;
		}
	}
}

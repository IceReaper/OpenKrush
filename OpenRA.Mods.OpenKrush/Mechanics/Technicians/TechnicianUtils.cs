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

namespace OpenRA.Mods.OpenKrush.Mechanics.Technicians;

using Bunkers.LobbyOptions;
using Bunkers.Traits;
using OpenRA.Traits;
using Traits;

public static class TechnicianUtils
{
	public static bool CanEnter(Actor source, Actor target, out bool blocked)
	{
		blocked = false;

		if (target.IsDead || target.Disposed || !target.IsInWorld)
			return false;

		var bunker = target.TraitOrDefault<TechBunker>();
		var usage = source.World.WorldActor.TraitOrDefault<TechBunkerUsage>();

		if (bunker != null && usage.Usage == TechBunkerUsageType.Technician)
		{
			blocked = bunker.State != TechBunkerState.ClosedUnlocked;

			return !blocked;
		}

		var trait = target.TraitOrDefault<TechnicianRepairable>();

		if (trait == null)
			return false;

		if (!trait.IsTraitDisabled && source.Owner.RelationshipWith(target.Owner) == PlayerRelationship.Ally)
			return true;

		blocked = true;

		return false;
	}
}

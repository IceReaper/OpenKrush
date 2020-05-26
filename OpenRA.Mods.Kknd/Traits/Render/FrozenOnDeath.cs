#region Copyright & License Information
/*
 * Copyright 2016-2018 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Render
{
	[Desc("This actor will be visible for a particular time when being killed.")]
	public class FrozenOnDeathInfo : ITraitInfo, Requires<HealthInfo>
	{
		[Desc("The amount of ticks the death state will be visible.")]
		public readonly int Duration = 25;

		public object Create(ActorInitializer init) { return new FrozenOnDeath(init, this); }
	}

	public class FrozenOnDeath : ITick
	{
		private int despawn;

		public FrozenOnDeath(ActorInitializer init, FrozenOnDeathInfo info)
		{
			despawn = info.Duration;
			/*
			// TODO refactor this!
			// init.Self.TraitOrDefault<Health>().RemoveOnDeath = false;
			*/
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsDead)
				return;

			if (--despawn <= 0)
				self.Dispose();
		}
	}
}

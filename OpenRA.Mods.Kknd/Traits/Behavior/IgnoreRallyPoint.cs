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

using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Behavior
{
	[Desc("Makes specific actors ignore the rally point when created.")]
	public class IgnoreRallyPointInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new IgnoreRallyPoint(); }
	}

	public class IgnoreRallyPoint : INotifyCreated
	{
		void INotifyCreated.Created(Actor self)
		{
			self.World.AddFrameEndTask(world =>
			{
				var activity = self.CurrentActivity;

				while (activity != null && !(activity is AttackMoveActivity))
					activity = activity.NextActivity;

				if (activity != null)
					activity.Cancel(self, true);
			});
		}
	}
}

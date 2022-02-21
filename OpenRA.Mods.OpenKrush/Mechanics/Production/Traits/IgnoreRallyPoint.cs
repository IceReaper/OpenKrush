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

namespace OpenRA.Mods.OpenKrush.Mechanics.Production.Traits;

using Common.Activities;
using JetBrains.Annotations;
using OpenRA.Traits;

[UsedImplicitly]
[Desc("Makes specific actors ignore the rally point when created.")]
public class IgnoreRallyPointInfo : TraitInfo
{
	public override object Create(ActorInitializer init)
	{
		return new IgnoreRallyPoint();
	}
}

public class IgnoreRallyPoint : INotifyCreated
{
	void INotifyCreated.Created(Actor self)
	{
		self.World.AddFrameEndTask(
			_ =>
			{
				var activity = self.CurrentActivity;

				while (activity != null && activity is not AttackMoveActivity)
					activity = activity.NextActivity;

				activity?.Cancel(self, true);
			}
		);
	}
}

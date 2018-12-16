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

namespace OpenRA.Mods.Kknd.Traits.Production
{
	[Desc("This special airstrike version makes bombers fly a base-to-target route.")]
	public class AdvancedAirstrikePowerInfo : AirstrikePowerInfo
	{
		public override object Create(ActorInitializer init) { return new AdvancedAirstrikePower(init.Self, this); }
	}

	public class AdvancedAirstrikePower : AirstrikePower
	{
		public AdvancedAirstrikePower(Actor self, AdvancedAirstrikePowerInfo info) : base(self, info) { }

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			var facing = (self.World.Map.CenterOfCell(order.TargetLocation) - self.CenterPosition).Yaw.Facing;
			SendAirstrike(self, self.World.Map.CenterOfCell(order.TargetLocation), false, facing);
		}
	}
}

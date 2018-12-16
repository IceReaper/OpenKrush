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

namespace OpenRA.Mods.Kknd.Traits.Technicians
{
	internal class RepairTask
	{
		public int Duration;
		public int Amount;

		public RepairTask(int amount, int duration)
		{
			Amount = amount;
			Duration = duration;
		}
	}
}

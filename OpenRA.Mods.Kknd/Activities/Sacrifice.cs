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
using OpenRA.Mods.Kknd.Traits.Altar;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Activities
{
	public class Sacrifice : Enter
	{
		public Sacrifice(Actor self, Target target, Color targetLineColor)
			: base(self, target, targetLineColor)
		{
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			targetActor.Trait<Altar>().Enter(self);
			self.Dispose();
		}
	}
}

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

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Kknd.Mechanics.Saboteurs.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Saboteurs.Activities
{
	public class SaboteurEnter : Enter
	{
		private readonly Actor target;

		public SaboteurEnter(Actor self, Target target, Color targetLineColor)
			: base(self, target, targetLineColor)
		{
			this.target = target.Actor;
		}

		public override bool Tick(Actor self)
		{
			if (!IsCanceling && !SaboteurUtils.CanEnter(self, target))
				Cancel(self, true);

			return base.Tick(self);
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			self.Trait<Saboteur>().Enter(self, targetActor);
			targetActor.Trait<SaboteurConquerable>().Enter(targetActor, self);
			self.Dispose();
		}
	}
}

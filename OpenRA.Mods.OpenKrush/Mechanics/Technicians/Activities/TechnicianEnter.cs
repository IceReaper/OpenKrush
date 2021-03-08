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

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.OpenKrush.Mechanics.Technicians.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenKrush.Mechanics.Technicians.Activities
{
	public class TechnicianEnter : Enter
	{
		private readonly Actor target;

		public TechnicianEnter(Actor self, Target target, Color targetLineColor)
			: base(self, target, targetLineColor)
		{
			this.target = target.Actor;
		}

		public override bool Tick(Actor self)
		{
			if (!IsCanceling && !TechnicianUtils.CanEnter(self, target))
				Cancel(self, true);

			return base.Tick(self);
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			self.Trait<Technician>().Enter(self, targetActor);
			targetActor.Trait<TechnicianRepairable>().Enter();
			self.Dispose();
		}
	}
}

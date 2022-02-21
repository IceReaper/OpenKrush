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

namespace OpenRA.Mods.OpenKrush.Mechanics.Saboteurs.Activities
{
	using Common.Activities;
	using OpenRA.Traits;
	using Primitives;
	using Traits;

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
			if (!this.IsCanceling && !SaboteurUtils.CanEnter(self, this.target, out _))
				this.Cancel(self, true);

			return base.Tick(self);
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			self.TraitOrDefault<Saboteur>().Enter(self, targetActor);
			targetActor.TraitOrDefault<SaboteurConquerable>().Enter(targetActor, self);
			self.Dispose();
		}
	}
}

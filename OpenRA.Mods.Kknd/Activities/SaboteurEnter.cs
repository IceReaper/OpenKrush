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
using OpenRA.Mods.Kknd.Traits.Saboteurs;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Activities
{
	public class SaboteurEnter : Enter
	{
		private readonly Actor target;
		private readonly SaboteurInfo saboteurInfo;

		public SaboteurEnter(Actor self, Actor target) : base(self, target, EnterBehaviour.Dispose)
		{
			this.target = target;
			saboteurInfo = self.Info.TraitInfo<SaboteurInfo>();
		}

		protected override void OnInside(Actor self)
		{
			var conquerable = target.Trait<SaboteurConquerable>();

			if (self.Owner == self.World.LocalPlayer)
			{
				if (target.Owner.Stances[self.Owner].HasStance(Stance.Ally))
					self.PlayVoice(saboteurInfo.VoiceEnterAlly);
				else if (conquerable.Population == 0)
					self.PlayVoice(saboteurInfo.VoiceConquered);
				else
					self.PlayVoice(saboteurInfo.VoiceEnterEnemy);
			}

			conquerable.Enter(target, self);
		}
	}
}

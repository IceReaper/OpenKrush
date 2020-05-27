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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Activities
{
	public class SaboteurEnter : Enter
	{
		public SaboteurEnter(Actor self, Target target, Color targetLineColor)
			: base(self, target, targetLineColor)
		{
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			var saboteurInfo = self.Info.TraitInfo<SaboteurInfo>();
			var saboteurConquerable = targetActor.Trait<SaboteurConquerable>();

			if (self.Owner == self.World.LocalPlayer)
			{
				if (targetActor.Owner.Stances[self.Owner].HasStance(Stance.Ally))
					self.PlayVoice(saboteurInfo.VoiceEnterAlly);
				else if (saboteurConquerable.Population == 0)
					self.PlayVoice(saboteurInfo.VoiceConquered);
				else
					self.PlayVoice(saboteurInfo.VoiceEnterEnemy);
			}

			saboteurConquerable.Enter(targetActor, self);
			self.Dispose();
		}
	}
}

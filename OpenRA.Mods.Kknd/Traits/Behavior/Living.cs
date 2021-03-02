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

using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Behavior
{
	[Desc("Makes infantry feel more alive by randomly rotating or playing an animation when idle.")]
	class LivingInfo : TraitInfo, Requires<MobileInfo>, Requires<WithSpriteBodyInfo>
	{
		[Desc("Chance per tick the actor rotates to a random direction.")]
		public readonly int RotationChance = 1000;

		[Desc("Chance per tick the actor triggers its bored sequence.")]
		public readonly int BoredChance = 5000;

		[Desc("Sequence to play when idle.")]
		public readonly string BoredSequence = "bored";

		public override object Create(ActorInitializer init) { return new Living(init, this); }
	}

	class Living : ITick
	{
		private readonly LivingInfo info;
		private readonly Mobile mobile;
		private readonly WithSpriteBody wsb;

		public Living(ActorInitializer init, LivingInfo info)
		{
			this.info = info;
			mobile = init.Self.Trait<Mobile>();
			wsb = init.Self.Trait<WithSpriteBody>();
		}

		void ITick.Tick(Actor self)
		{
			if (self.CurrentActivity == null)
			{
				if (info.RotationChance > 0 && self.World.SharedRandom.Next(1, info.RotationChance) == 1)
					mobile.Facing = WAngle.FromFacing(self.World.SharedRandom.Next(0x00, 0xff));

				if (info.BoredSequence != null && self.World.SharedRandom.Next(1, info.BoredChance) == 1)
					wsb.PlayCustomAnimation(self, info.BoredSequence);
			}
		}
	}
}

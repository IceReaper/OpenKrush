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

namespace OpenRA.Mods.OpenKrush.Traits.Behavior
{
	using Common.Traits;
	using OpenRA.Traits;

	[Desc("Play together with an attack.")]
	public class WithLoopedAttackSoundInfo : TraitInfo
	{
		[FieldLoader.RequireAttribute]
		[Desc("Sound filename to use")]
		public readonly string[] Report = null;

		public readonly int Delay = 10;

		public override object Create(ActorInitializer init)
		{
			return new WithLoopedAttackSound(this);
		}
	}

	public class WithLoopedAttackSound : INotifyAttack, ITick, INotifyRemovedFromWorld
	{
		readonly WithLoopedAttackSoundInfo info;

		ISound sound;
		int tick;

		public WithLoopedAttackSound(WithLoopedAttackSoundInfo info)
		{
			this.info = info;
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (sound == null)
				sound = Game.Sound.PlayLooped(SoundType.World, info.Report.Random(self.World.SharedRandom), self.CenterPosition);

			tick = self.World.WorldTick + info.Delay;
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel)
		{
		}

		void ITick.Tick(Actor self)
		{
			if (sound == null || tick >= self.World.WorldTick)
				return;

			Game.Sound.StopSound(sound);
			sound = null;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (sound != null)
				Game.Sound.StopSound(sound);
		}
	}
}

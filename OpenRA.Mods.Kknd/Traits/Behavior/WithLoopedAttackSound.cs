using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Behavior
{
	[Desc("Play together with an attack.")]
	public class WithLoopedAttackSoundInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("Sound filename to use")]
		public readonly string[] Report = null;

		public readonly int Delay = 10;

		public object Create(ActorInitializer init)
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

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (sound == null)
				sound = Game.Sound.PlayLooped(SoundType.World, info.Report.Random(self.World.SharedRandom), self.CenterPosition);

			tick = self.World.WorldTick + info.Delay;
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
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

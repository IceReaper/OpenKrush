using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Kknd.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.SpritesWithOffsets
{
	[Desc("Use asset provided armament offset.")]
	public class OffsetsArmamentInfo : ArmamentInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Offset id to use per burst shot.")]
		public readonly int[] BurstOffsets = { 0 };

		public override object Create(ActorInitializer init) { return new OffsetsArmament(init.Self, this); }
	}

	public class OffsetsArmament : Armament
	{
		readonly OffsetsArmamentInfo info;
		readonly WithSpriteBody wsb;

		public OffsetsArmament(Actor self, OffsetsArmamentInfo info) : base(self, info)
		{
			this.info = info;
			wsb = self.Trait<WithSpriteBody>();
		}

		protected override WVec CalculateMuzzleOffset(Actor self, Barrel barrel)
		{
			var offset = base.CalculateMuzzleOffset(self, barrel);

			var sequence = wsb.DefaultAnimation.CurrentSequence as OffsetsSpriteSequence;
			if (sequence != null)
			{
				var wst = self.TraitOrDefault<WithOffsetsSpriteTurret>();
				var weaponPoint = info.BurstOffsets[(Weapon.Burst - Burst) % info.BurstOffsets.Length];

				var sprite = wsb.DefaultAnimation.Image;
				if (sequence.EmbeddedOffsets.ContainsKey(sprite) && sequence.EmbeddedOffsets[sprite] != null)
				{
					var offsets = sequence.EmbeddedOffsets[sprite];
					var weaponOrTurretOffset = offsets.FirstOrDefault(p => p.Id == (wst == null ? weaponPoint : 0));

					if (weaponOrTurretOffset != null)
						offset = new WVec(offset.X + weaponOrTurretOffset.X * 32, offset.Y + weaponOrTurretOffset.Y * 32, offset.Z);
				}

				if (wst != null)
				{
					var turretSequence = wst.DefaultAnimation.CurrentSequence as OffsetsSpriteSequence;
					if (turretSequence != null)
					{
						sprite = wst.DefaultAnimation.Image;
						if (turretSequence.EmbeddedOffsets.ContainsKey(sprite) && turretSequence.EmbeddedOffsets[sprite] != null)
						{
							var offsets = turretSequence.EmbeddedOffsets[sprite];
							var weaponOffset = offsets.FirstOrDefault(p => p.Id == weaponPoint);

							if (weaponOffset != null)
								offset = new WVec(offset.X + weaponOffset.X * 32, offset.Y + weaponOffset.Y * 32, offset.Z);
						}
					}
				}
			}

			return offset;
		}
	}
}

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Warheads
{
    public class ShrapnelCreateEffectWarhead : CreateEffectWarhead
    {
	    public readonly int2 Radius = int2.Zero;

        [Desc("Weapon to fire when this warhead triggers.")]
        public readonly string ShrapnelWeapon = null;

        [Desc("The minimum and maximum distances the shrapnel may travel.")]
        public readonly WDist[] ShrapnelRange = { WDist.Zero, WDist.Zero };
        
		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (!target.IsValidFor(firedBy))
				return;

			var random = firedBy.World.SharedRandom;
			var pos = target.CenterPosition + new WVec(Radius.X == 0 ? 0 : random.Next(-Radius.X, Radius.X), Radius.Y == 0 ? 0 : random.Next(-Radius.Y, Radius.Y), 0);
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);
			var isValid = IsValidImpact(pos, firedBy);

			if ((!world.Map.Contains(targetTile)) || (!isValid))
				return;

			var palette = ExplosionPalette;
			if (UsePlayerPalette)
				palette += firedBy.Owner.InternalName;

			if (ForceDisplayAtGroundLevel)
			{
				var dat = world.Map.DistanceAboveTerrain(pos);
				pos = new WPos(pos.X, pos.Y, pos.Z - dat.Length);
			}

			var explosion = Explosions.RandomOrDefault(Game.CosmeticRandom);
			if (Image != null && explosion != null)
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, w, Image, explosion, palette)));

			if (ShrapnelWeapon != null)
			{
				WeaponInfo weaponInfo;
				var weaponToLower = ShrapnelWeapon.ToLowerInvariant();

				if (!Game.ModData.DefaultRules.Weapons.TryGetValue(weaponToLower, out weaponInfo))
					throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

				var rotation = WRot.FromFacing(world.SharedRandom.Next(1024));
				var range = world.SharedRandom.Next(ShrapnelRange[0].Length, ShrapnelRange[1].Length);
				var passiveTarget = pos + new WVec(range, 0, 0).Rotate(rotation);

				var args = new ProjectileArgs
				{
					Weapon = weaponInfo,
					DamageModifiers = new int[0],
					InaccuracyModifiers = new int[0],
					RangeModifiers= new int[0],
					Source = pos,
					CurrentSource = () => pos,
					SourceActor = firedBy,
					PassiveTarget = passiveTarget,
					GuidedTarget = target
				};

				world.AddFrameEndTask(x =>
				{
					if (args.Weapon.Projectile != null)
					{
						var projectile = args.Weapon.Projectile.Create(args);
						if (projectile != null)
							world.Add(projectile);
					}
					else
					{
						foreach (var warhead in args.Weapon.Warheads.Keys)
						{
							var wh = warhead; // force the closure to bind to the current warhead

							if (wh.Delay > 0)
								firedBy.World.AddFrameEndTask(w => w.Add(new DelayedImpact(wh.Delay, wh, Target.FromPos(args.PassiveTarget), args.SourceActor, new int[0])));
							else
								wh.DoImpact(Target.FromPos(args.PassiveTarget), args.SourceActor, new int[0]);
						}
					}
				});
			}

			var impactSound = ImpactSounds.RandomOrDefault(Game.CosmeticRandom);
			if (impactSound != null && Game.CosmeticRandom.Next(0, 100) < ImpactSoundChance)
				Game.Sound.Play(SoundType.World, impactSound, pos);
		}
    }
}
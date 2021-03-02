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

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var random = args.SourceActor.World.SharedRandom;
			var pos = target.CenterPosition + new WVec(Radius.X == 0 ? 0 : random.Next(-Radius.X, Radius.X), Radius.Y == 0 ? 0 : random.Next(-Radius.Y, Radius.Y), 0);
			var world = args.SourceActor.World;
			var targetTile = world.Map.CellContaining(pos);

			if ((!world.Map.Contains(targetTile)))
				return;

			var palette = ExplosionPalette;
			if (UsePlayerPalette)
				palette += args.SourceActor.Owner.InternalName;

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

				var newArgs = new ProjectileArgs
				{
					Weapon = weaponInfo,
					DamageModifiers = new int[0],
					InaccuracyModifiers = new int[0],
					RangeModifiers = new int[0],
					Source = pos,
					CurrentSource = () => pos,
					SourceActor = args.SourceActor,
					PassiveTarget = passiveTarget,
					GuidedTarget = target
				};

				world.AddFrameEndTask(x =>
				{
					if (newArgs.Weapon.Projectile != null)
					{
						var projectile = newArgs.Weapon.Projectile.Create(newArgs);
						if (projectile != null)
							world.Add(projectile);
					}
					else
					{
						foreach (var warhead in newArgs.Weapon.Warheads)
						{
							var wh = warhead; // force the closure to bind to the current warhead
							var iargs = new WarheadArgs { SourceActor = newArgs.SourceActor };

							if (wh.Delay > 0)
								args.SourceActor.World.AddFrameEndTask(w => w.Add(new DelayedImpact(wh.Delay, wh, Target.FromPos(newArgs.PassiveTarget), iargs)));
							else
								wh.DoImpact(Target.FromPos(newArgs.PassiveTarget), iargs);
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

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

namespace OpenRA.Mods.OpenKrush.Mechanics.Misc.Warheads
{
	using Common.Effects;
	using Common.Warheads;
	using Effects;
	using GameRules;
	using JetBrains.Annotations;
	using OpenRA.Traits;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class ShrapnelCreateEffectWarhead : CreateEffectWarhead
	{
		public readonly int2 Radius = int2.Zero;

		[Desc("Weapon to fire when this warhead triggers.")]
		public readonly string? ShrapnelWeapon;

		[Desc("The minimum and maximum distances the shrapnel may travel.")]
		public readonly WDist[] ShrapnelRange = { WDist.Zero, WDist.Zero };

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			if (args.WeaponTarget.Actor != null && !this.IsValidAgainst(args.WeaponTarget.Actor, args.SourceActor))
				return;

			var random = args.SourceActor.World.SharedRandom;

			var pos = target.CenterPosition
				+ new WVec(
					this.Radius.X == 0 ? 0 : random.Next(-this.Radius.X, this.Radius.X),
					this.Radius.Y == 0 ? 0 : random.Next(-this.Radius.Y, this.Radius.Y),
					0
				);

			var world = args.SourceActor.World;
			var targetTile = world.Map.CellContaining(pos);

			if (!world.Map.Contains(targetTile))
				return;

			var palette = this.ExplosionPalette;

			if (this.UsePlayerPalette)
				palette += args.SourceActor.Owner.InternalName;

			if (this.ForceDisplayAtGroundLevel)
			{
				var dat = world.Map.DistanceAboveTerrain(pos);
				pos = new(pos.X, pos.Y, pos.Z - dat.Length);
			}

			var explosion = this.Explosions.RandomOrDefault(Game.CosmeticRandom);

			if (this.Image != null && explosion != null)
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, w, this.Image, explosion, palette)));

			if (this.ShrapnelWeapon != null)
			{
				var weaponToLower = this.ShrapnelWeapon.ToLowerInvariant();

				if (!Game.ModData.DefaultRules.Weapons.TryGetValue(weaponToLower, out var weaponInfo))
					throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

				var rotation = WRot.FromFacing(world.SharedRandom.Next(1024));
				var range = world.SharedRandom.Next(this.ShrapnelRange[0].Length, this.ShrapnelRange[1].Length);
				var passiveTarget = pos + new WVec(range, 0, 0).Rotate(rotation);

				var newArgs = new ProjectileArgs
				{
					Weapon = weaponInfo,
					DamageModifiers = Array.Empty<int>(),
					InaccuracyModifiers = Array.Empty<int>(),
					RangeModifiers = Array.Empty<int>(),
					Source = pos,
					CurrentSource = () => pos,
					SourceActor = args.SourceActor,
					PassiveTarget = passiveTarget,
					GuidedTarget = target
				};

				world.AddFrameEndTask(
					_ =>
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
								{
									args.SourceActor.World.AddFrameEndTask(
										w => w.Add(new DelayedImpact(wh.Delay, wh, Target.FromPos(newArgs.PassiveTarget), iargs))
									);
								}
								else
									wh.DoImpact(Target.FromPos(newArgs.PassiveTarget), iargs);
							}
						}
					}
				);
			}

			var impactSound = this.ImpactSounds.RandomOrDefault(Game.CosmeticRandom);

			if (impactSound != null && Game.CosmeticRandom.Next(0, 100) < this.ImpactSoundChance)
				Game.Sound.Play(SoundType.World, impactSound, pos);
		}
	}
}

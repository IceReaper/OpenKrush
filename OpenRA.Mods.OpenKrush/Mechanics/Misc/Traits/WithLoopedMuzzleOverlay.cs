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

namespace OpenRA.Mods.OpenKrush.Mechanics.Misc.Traits
{
	using Common.Traits;
	using Common.Traits.Render;
	using Graphics;
	using JetBrains.Annotations;
	using OpenRA.Traits;
	using Primitives;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[Desc("Renders the MuzzleSequence from the Armament trait.")]
	public class WithLoopedMuzzleOverlayInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<AttackBaseInfo>, Requires<ArmamentInfo>
	{
		[Desc("Ignore the weapon position, and always draw relative to the center of the actor")]
		public readonly bool IgnoreOffset;

		public override object Create(ActorInitializer init)
		{
			return new WithLoopedMuzzleOverlay(init.Self, this);
		}
	}

	public class WithLoopedMuzzleOverlay : ConditionalTrait<WithLoopedMuzzleOverlayInfo>, INotifyAttack, IRender, ITick
	{
		private readonly Dictionary<Barrel, int> visible = new();
		private readonly Dictionary<Barrel, AnimationWithOffset> anims = new();
		private readonly Armament[] armaments;

		public WithLoopedMuzzleOverlay(Actor self, WithLoopedMuzzleOverlayInfo info)
			: base(info)
		{
			var render = self.TraitOrDefault<RenderSprites>();
			var facing = self.TraitOrDefault<IFacing>();

			this.armaments = self.TraitsImplementing<Armament>().Where(arm => arm.Info.MuzzleSequence != null).ToArray();

			foreach (var arm in this.armaments)
			{
				foreach (var b in arm.Barrels)
				{
					var barrel = b;
					var turreted = self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.Name == arm.Info.Turret);

					var getFacting = new Func<WAngle>(() => WAngle.Zero);

					if (turreted != null)
						getFacting = () => turreted.WorldOrientation.Yaw;
					else if (facing != null)
						getFacting = () => facing.Facing;

					var muzzleFlash = new Animation(self.World, render.GetImage(self), getFacting);
					this.visible.Add(barrel, 0);

					this.anims.Add(
						barrel,
						new(
							muzzleFlash,
							() => info.IgnoreOffset ? WVec.Zero : arm.MuzzleOffset(self, barrel),
							() => this.IsTraitDisabled || this.visible[barrel] == 0,
							p => RenderUtils.ZOffsetFromCenter(self, p, 2)
						)
					);
				}
			}
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament? a, Barrel? barrel)
		{
			if (a == null || barrel == null || !this.armaments.Contains(a))
				return;

			var sequence = a.Info.MuzzleSequence;

			if (this.visible[barrel] == 0)
				this.anims[barrel].Animation.PlayThen(sequence, () => this.visible[barrel] = 0);

			this.visible[barrel] = 2;
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel)
		{
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			foreach (var arm in this.armaments)
			{
				var palette = wr.Palette(arm.Info.MuzzlePalette);

				foreach (var barrel in arm.Barrels)
				{
					var anim = this.anims[barrel];

					if (anim.DisableFunc != null && anim.DisableFunc())
						continue;

					foreach (var r in anim.Render(self, wr, palette))
						yield return r;
				}
			}
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			// Muzzle flashes don't contribute to actor bounds
			yield break;
		}

		void ITick.Tick(Actor self)
		{
			foreach (var barrel in this.visible.Keys)
				this.visible[barrel] -= this.visible[barrel] > 1 ? 1 : 0;

			foreach (var a in this.anims.Values)
				a.Animation.Tick();
		}
	}
}

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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Kknd.Graphics;

namespace OpenRA.Mods.Kknd.Traits.SpritesWithOffsets
{
	[Desc("Use asset provided turret offset.")]
	public class WithOffsetsSpriteTurretInfo : WithSpriteTurretInfo, IRenderActorPreviewSpritesInfo
	{
		public override object Create(ActorInitializer init) { return new WithOffsetsSpriteTurret(init.Self, this); }

		public new IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var t = init.Actor.TraitInfos<TurretedInfo>()
				.First(tt => tt.Turret == Turret);

			var bodyAnim = new Animation(init.World, image, init.GetFacing());
			bodyAnim.PlayRepeating(RenderSprites.NormalizeSequence(bodyAnim, init.GetDamageState(), "idle"));
			var bodySequence = bodyAnim.CurrentSequence as OffsetsSpriteSequence;
			Func<WVec> offset = null;

			if (bodySequence != null && bodySequence.EmbeddedOffsets.ContainsKey(bodyAnim.Image) && bodySequence.EmbeddedOffsets[bodyAnim.Image] != null)
			{
				var point = bodySequence.EmbeddedOffsets[bodyAnim.Image].FirstOrDefault(p1 => p1.Id == 0);

				if (point != null)
					offset = () => new WVec(point.X * 32, point.Y * 32, 0);
			}

			var turretFacing = t.WorldFacingFromInit(init);
			var anim = new Animation(init.World, image, turretFacing);
			anim.Play(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			if (offset == null)
			{
				var facing = init.GetFacing();
				Func<WRot> orientation = () => body.QuantizeOrientation(WRot.FromYaw(facing()), facings);
				offset = () => body.LocalToWorld(t.Offset.Rotate(orientation()));
			}

			Func<int> zOffset = () =>
			{
				var tmpOffset = offset();
				return -(tmpOffset.Y + tmpOffset.Z) + 1;
			};

			if (IsPlayerPalette)
				p = init.WorldRenderer.Palette(Palette + init.Get<OwnerInit>().InternalName);
			else if (Palette != null)
				p = init.WorldRenderer.Palette(Palette);

			yield return new SpriteActorPreview(anim, offset, zOffset, p);
		}
	}

	public class WithOffsetsSpriteTurret : WithSpriteTurret
	{
		readonly WithSpriteBody wsb;

		public WithOffsetsSpriteTurret(Actor self, WithSpriteTurretInfo info)
			: base(self, info)
		{
			wsb = self.Trait<WithSpriteBody>();
		}

		protected override WVec TurretOffset(Actor self)
		{
			var sequence = wsb.DefaultAnimation.CurrentSequence as OffsetsSpriteSequence;
			var sprite = wsb.DefaultAnimation.Image;

			if (sequence != null && sequence.EmbeddedOffsets.ContainsKey(sprite) && sequence.EmbeddedOffsets[sprite] != null)
			{
				var point = sequence.EmbeddedOffsets[sprite].FirstOrDefault(p => p.Id == 0);

				if (point != null)
					return new WVec(point.X * 32, point.Y * 32, base.TurretOffset(self).Z);
			}

			return base.TurretOffset(self);
		}
	}
}

#region Copyright & License Information

/*
 * Copyright 2007-2022 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Mechanics.DataFromAssets.Traits;

using Common.Graphics;
using Common.Traits;
using Common.Traits.Render;
using Graphics;
using JetBrains.Annotations;
using OpenRA.Graphics;

[UsedImplicitly]
[Desc("Use asset provided turret offset.")]
public class WithOffsetsSpriteTurretInfo : WithSpriteTurretInfo, IRenderActorPreviewSpritesInfo
{
	public override object Create(ActorInitializer init)
	{
		return new WithOffsetsSpriteTurret(init.Self, this);
	}

	public new IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
	{
		if (!this.EnabledByDefault)
			yield break;

		var body = init.Actor.TraitInfoOrDefault<BodyOrientationInfo>();
		var turretedInfo = init.Actor.TraitInfos<TurretedInfo>().FirstOrDefault(tt => tt.Turret == this.Turret);

		if (turretedInfo == null)
			yield break;

		var facing = init.GetFacing();
		var offset = new Func<WVec>(() => body.LocalToWorld(turretedInfo.Offset.Rotate(body.QuantizeOrientation(WRot.FromYaw(facing()), facings))));

		var bodyAnim = new Animation(init.World, image, init.GetFacing());
		bodyAnim.PlayRepeating(RenderSprites.NormalizeSequence(bodyAnim, init.GetDamageState(), "idle"));

		if (bodyAnim.CurrentSequence is OffsetsSpriteSequence bodySequence && bodySequence.EmbeddedOffsets.TryGetValue(bodyAnim.Image, out var imageOffset))
		{
			var point = imageOffset.FirstOrDefault(p1 => p1.Id == 0);

			if (point != null)
				offset = () => new(point.X * 32, point.Y * 32, 0);
		}

		if (this.IsPlayerPalette)
			p = init.WorldRenderer.Palette(this.Palette + init.Get<OwnerInit>().InternalName);
		else if (this.Palette != null)
			p = init.WorldRenderer.Palette(this.Palette);

		var turretFacing = turretedInfo.WorldFacingFromInit(init);
		var anim = new Animation(init.World, image, turretFacing);
		anim.Play(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), this.Sequence));

		yield return new SpriteActorPreview(
			anim,
			offset,
			() =>
			{
				var tmpOffset = offset();

				return -(tmpOffset.Y + tmpOffset.Z) + 1;
			},
			p
		);
	}
}

public class WithOffsetsSpriteTurret : WithSpriteTurret
{
	private readonly WithSpriteBody wsb;

	public WithOffsetsSpriteTurret(Actor self, WithSpriteTurretInfo info)
		: base(self, info)
	{
		this.wsb = self.TraitOrDefault<WithSpriteBody>();
	}

	protected override WVec TurretOffset(Actor self)
	{
		var sequence = this.wsb.DefaultAnimation.CurrentSequence as OffsetsSpriteSequence;
		var sprite = this.wsb.DefaultAnimation.Image;

		if (sequence == null || !sequence.EmbeddedOffsets.ContainsKey(sprite))
			return base.TurretOffset(self);

		var point = sequence.EmbeddedOffsets[sprite].FirstOrDefault(p => p.Id == 0);

		return point != null ? new(point.X * 32, point.Y * 32, base.TurretOffset(self).Z) : base.TurretOffset(self);
	}
}

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

namespace OpenRA.Mods.OpenKrush.Mechanics.DataFromAssets.Traits;

using Common.Traits;
using Common.Traits.Render;
using Graphics;
using JetBrains.Annotations;
using OpenRA.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("Use asset provided armament offset.")]
public class OffsetsArmamentInfo : ArmamentInfo, Requires<WithSpriteBodyInfo>
{
	[Desc("Offset id to use per burst shot.")]
	public readonly int[] BurstOffsets = { 0 };

	public override object Create(ActorInitializer init)
	{
		return new OffsetsArmament(init.Self, this);
	}
}

public class OffsetsArmament : Armament
{
	private readonly OffsetsArmamentInfo info;
	private readonly WithSpriteBody wsb;

	public OffsetsArmament(Actor self, OffsetsArmamentInfo info)
		: base(self, info)
	{
		this.info = info;
		this.wsb = self.TraitOrDefault<WithSpriteBody>();
	}

	protected override WVec CalculateMuzzleOffset(Actor self, Barrel barrel)
	{
		var offset = base.CalculateMuzzleOffset(self, barrel);

		if (this.wsb.DefaultAnimation.CurrentSequence is not OffsetsSpriteSequence sequence)
			return offset;

		var wst = self.TraitOrDefault<WithOffsetsSpriteTurret>();
		var weaponPoint = this.info.BurstOffsets[(this.Weapon.Burst - this.Burst) % this.info.BurstOffsets.Length];

		var sprite = this.wsb.DefaultAnimation.Image;

		if (sequence.EmbeddedOffsets.ContainsKey(sprite))
		{
			var offsets = sequence.EmbeddedOffsets[sprite];
			var weaponOrTurretOffset = offsets.FirstOrDefault(p => p.Id == (wst == null ? weaponPoint : 0));

			if (weaponOrTurretOffset != null)
				offset = new(offset.X + weaponOrTurretOffset.X * 32, offset.Y + weaponOrTurretOffset.Y * 32, offset.Z);
		}

		if (wst?.DefaultAnimation.CurrentSequence is not OffsetsSpriteSequence turretSequence)
			return offset;

		sprite = wst.DefaultAnimation.Image;

		if (!turretSequence.EmbeddedOffsets.ContainsKey(sprite))
			return offset;

		var turretOffsets = turretSequence.EmbeddedOffsets[sprite];
		var weaponOffset = turretOffsets.FirstOrDefault(p => p.Id == weaponPoint);

		if (weaponOffset != null)
			offset = new(offset.X + weaponOffset.X * 32, offset.Y + weaponOffset.Y * 32, offset.Z);

		return offset;
	}
}

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

namespace OpenRA.Mods.OpenKrush.Mechanics.Misc.Traits;

using Common.Traits;
using Common.Traits.Render;
using JetBrains.Annotations;
using OpenRA.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("Makes infantry feel more alive by randomly rotating or playing an animation when idle.")]
public class LivingInfo : TraitInfo, Requires<MobileInfo>, Requires<WithSpriteBodyInfo>
{
	[Desc("Chance per tick the actor rotates to a random direction.")]
	public readonly int RotationChance = 1000;

	[Desc("Chance per tick the actor moves to a different free subcell in its cell.")]
	public readonly int SubcellMoveChance = 1000;

	[Desc("Chance per tick the actor triggers its bored sequence.")]
	public readonly int BoredChance = 5000;

	[Desc("Sequence to play when idle.")]
	public readonly string? BoredSequence = "bored";

	public override object Create(ActorInitializer init)
	{
		return new Living(init, this);
	}
}

public class Living : ITick
{
	private readonly LivingInfo info;
	private readonly Mobile mobile;
	private readonly WithSpriteBody withSpriteBody;

	public Living(ActorInitializer init, LivingInfo info)
	{
		this.info = info;
		this.mobile = init.Self.TraitOrDefault<Mobile>();
		this.withSpriteBody = init.Self.TraitOrDefault<WithSpriteBody>();
	}

	void ITick.Tick(Actor self)
	{
		if (self.CurrentActivity != null)
			return;

		if (this.info.BoredSequence != null && self.World.SharedRandom.Next(0, this.info.BoredChance) == 0)
			this.withSpriteBody.PlayCustomAnimation(self, this.info.BoredSequence);
		else if (this.info.RotationChance > 0 && self.World.SharedRandom.Next(0, this.info.RotationChance) == 0)
			this.mobile.Facing = WAngle.FromFacing(self.World.SharedRandom.Next(0x00, 0xff));
		else if (this.mobile.Info.LocomotorInfo.SharesCell
			&& this.info.SubcellMoveChance > 0
			&& self.World.SharedRandom.Next(0, this.info.SubcellMoveChance) == 0)
		{
			// TODO openra does not support Move from subcell to subcell in the same cell.
		}
	}
}

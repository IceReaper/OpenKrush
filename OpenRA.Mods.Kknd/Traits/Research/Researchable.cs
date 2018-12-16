#region Copyright & License Information
/*
 * Copyright 2016-2018 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Research
{
	[Desc("Makes an actor researchable.")]
	class ResearchableInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Research sequence name to use.")]
		[SequenceReference] public readonly string Sequence = "research";

		[Desc("Initial tech level.")]
		public readonly int Level = 0;

		[Desc("Maximum tech level.")]
		public readonly int MaxLevel = 5;

		[Desc("Duration of research per level-up.")]
		public readonly int[] ResearchTime = { 400, 700, 1000, 1250, 1500 };

		[Desc("Costs of research per level-up.")]
		public readonly int[] ResearchCost = { 250, 500, 1000, 1500, 2000 };

		[Desc("Offset for the research sequence.")]
		public readonly int2 Offset = int2.Zero;

		public override object Create(ActorInitializer init) { return new Researchable(init, this); }
	}

	class Researchable : ConditionalTrait<ResearchableInfo>
	{
		private readonly ResearchableInfo info;

		private readonly Animation overlay;
		public int Level;
		public Researches Researches;

		public Researchable(ActorInitializer init, ResearchableInfo info) : base(info)
		{
			this.info = info;
			Level = info.Level;

			var rs = init.Self.Trait<RenderSprites>();
			var body = init.Self.Trait<BodyOrientation>();

			var hidden = new Func<bool>(() => Researches == null || init.Self.Owner.IsAlliedWith(init.World.LocalPlayer));

			overlay = new Animation(init.World, "indicators", hidden);
			overlay.PlayRepeating(this.info.Sequence + 0);

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(new WVec(info.Offset.Y * -32, info.Offset.X * -32, 0).Rotate(body.QuantizeOrientation(init.Self, init.Self.Orientation))),
				hidden,
				p => RenderUtils.ZOffsetFromCenter(init.Self, p, 1));

			rs.Add(anim);
		}

		public void SetProgress(int progress)
		{
			if (overlay.CurrentSequence.Name != info.Sequence + progress)
				overlay.PlayRepeating(info.Sequence + progress);
		}
	}
}

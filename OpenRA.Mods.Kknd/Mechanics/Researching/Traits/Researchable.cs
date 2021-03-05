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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Researching.Traits
{
	[Desc("KKnD Research mechanism, attach to the actor which has tech levels.")]
	public class ResearchableInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Research sequence name to use.")]
		public readonly string Sequence = "research";

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

		public override object Create(ActorInitializer init)
		{
			return new Researchable(init, this);
		}
	}

	public class Researchable : ConditionalTrait<ResearchableInfo>
	{
		private readonly ResearchableInfo info;
		private readonly Actor self;

		private readonly Animation overlay;
		private readonly int researchSteps;

		public int Level;
		public Researches ResearchedBy;

		public Researchable(ActorInitializer init, ResearchableInfo info)
			: base(info)
		{
			this.info = info;
			self = init.Self;
			Level = info.Level;

			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			var hidden = new Func<bool>(() => ResearchedBy == null || !self.Owner.IsAlliedWith(self.World.LocalPlayer));

			overlay = new Animation(self.World, "indicators", hidden);
			overlay.PlayRepeating(info.Sequence + 0);

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(
					new WVec(info.Offset.Y * -32, info.Offset.X * -32, 0).Rotate(body.QuantizeOrientation(self, self.Orientation))),
				hidden,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			rs.Add(anim);

			while (overlay.HasSequence(info.Sequence + researchSteps))
				researchSteps++;
		}

		public void SetProgress(float progress)
		{
			var sequence = info.Sequence + (int)Math.Floor(researchSteps * progress);

			if (overlay.CurrentSequence.Name != sequence)
				overlay.PlayRepeating(sequence);
		}

		public ResarchState GetState()
		{
			if (IsTraitDisabled)
				return ResarchState.Unavailable;

			if (self.IsDead || !self.IsInWorld)
				return ResarchState.Unavailable;

			if (ResearchedBy != null)
				return ResarchState.Researching;

			return ResarchState.Available;
		}
	}
}

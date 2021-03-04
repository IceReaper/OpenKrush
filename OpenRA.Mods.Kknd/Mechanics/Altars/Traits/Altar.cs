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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Kknd.Traits.Production;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Altars.Traits
{
	[Desc("Allow sacrificeable units to enter and spawn a new actor..")]
	public class AltarInfo : AdvancedProductionInfo, Requires<RenderSpritesInfo>
	{
		[Desc("The amount of units required for a sacrifice.")]
		public readonly int Sacrifices = 5;

		[Desc("Duration of the sacrifice.")]
		public readonly int Duration = 50;

		[FieldLoader.RequireAttribute]
		[Desc("The unit which is granted upon sacrificing.")]
		public readonly string Summon = null;

		[Desc("Sequence to be played when sacrificing.")]
		[SequenceReference]
		public readonly string SequenceEnter = null;

		[Desc("Sequence to be played when summoning.")]
		[SequenceReference]
		public readonly string SequenceSummon = null;

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		public override object Create(ActorInitializer init) { return new Altar(init, this); }
	}

	public class Altar : AdvancedProduction, ITick
	{
		private readonly AltarInfo info;

		private int sacrificeTicker;
		private int summonTicker;
		private int population;

		public Altar(ActorInitializer init, AltarInfo info)
			: base(init, info)
		{
			this.info = info;

			var renderSprites = init.Self.Trait<RenderSprites>();
			var body = init.Self.Trait<BodyOrientation>();

			if (info.SequenceEnter != null)
			{
				var animation = new Animation(init.Self.World, renderSprites.GetImage(init.Self));
				animation.PlayRepeating(RenderSprites.NormalizeSequence(animation, init.Self.GetDamageState(), info.SequenceEnter));

				var animationWithOffset = new AnimationWithOffset(animation,
					() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(init.Self, init.Self.Orientation))),
					() => IsTraitDisabled || sacrificeTicker == 0,
					p => RenderUtils.ZOffsetFromCenter(init.Self, p, 1));

				renderSprites.Add(animationWithOffset);
			}

			if (info.SequenceSummon != null)
			{
				var animation = new Animation(init.Self.World, renderSprites.GetImage(init.Self));
				animation.PlayRepeating(RenderSprites.NormalizeSequence(animation, init.Self.GetDamageState(), info.SequenceSummon));

				var animationWithOffset = new AnimationWithOffset(animation,
					() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(init.Self, init.Self.Orientation))),
					() => IsTraitDisabled || summonTicker == 0,
					p => RenderUtils.ZOffsetFromCenter(init.Self, p, 1));

				renderSprites.Add(animationWithOffset);
			}
		}

		public void Enter()
		{
			population++;
			sacrificeTicker = info.Duration;
		}

		void ITick.Tick(Actor self)
		{
			if (sacrificeTicker > 0)
				sacrificeTicker--;

			if (summonTicker > 0)
			{
				if (--summonTicker != 0)
					return;

				var numSummons = population / info.Sacrifices;
				population -= numSummons * info.Sacrifices;

				self.World.AddFrameEndTask(w =>
				{
					var td = new TypeDictionary
					{
						new OwnerInit(self.Owner)
					};

					for (var i = 0; i < numSummons; i++)
						Produce(self, self.World.Map.Rules.Actors[info.Summon], "produce", td, 0);
				});
			}
			else if (population == info.Sacrifices)
				summonTicker = info.Duration;
		}
	}
}

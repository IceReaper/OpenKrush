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

namespace OpenRA.Mods.OpenKrush.Mechanics.Sacrificing.Traits;

using Common.Traits;
using Common.Traits.Render;
using Graphics;
using JetBrains.Annotations;
using OpenRA.Traits;
using Primitives;
using Production.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("Allow sacrificeable units to enter and spawn a new actor..")]
public class SacrificerInfo : AdvancedProductionInfo, Requires<RenderSpritesInfo>
{
	[Desc("The amount of units required for a sacrifice.")]
	public readonly int Sacrifices = 5;

	[Desc("Duration of the sacrifice.")]
	public readonly int Duration = 50;

	[FieldLoader.RequireAttribute]
	[Desc("The unit which is granted upon sacrificing.")]
	public readonly string Summon = "";

	[Desc("Sequence to be played when sacrificing.")]
	[SequenceReference]
	public readonly string? SequenceEnter;

	[Desc("Sequence to be played when summoning.")]
	[SequenceReference]
	public readonly string? SequenceSummon;

	[Desc("Position relative to body")]
	public readonly WVec Offset = WVec.Zero;

	public override object Create(ActorInitializer init)
	{
		return new Sacrificer(init, this);
	}
}

public class Sacrificer : AdvancedProduction, ITick
{
	private readonly SacrificerInfo info;

	private int sacrificeTicker;
	private int summonTicker;
	private int population;

	public Sacrificer(ActorInitializer init, SacrificerInfo info)
		: base(init, info)
	{
		this.info = info;

		var renderSprites = init.Self.TraitOrDefault<RenderSprites>();
		var body = init.Self.TraitOrDefault<BodyOrientation>();

		if (info.SequenceEnter != null)
		{
			var animationEnter = new Animation(init.Self.World, renderSprites.GetImage(init.Self));
			animationEnter.PlayRepeating(RenderSprites.NormalizeSequence(animationEnter, init.Self.GetDamageState(), info.SequenceEnter));

			renderSprites.Add(
				new(
					animationEnter,
					() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(init.Self, init.Self.Orientation))),
					() => this.IsTraitDisabled || this.sacrificeTicker == 0,
					p => RenderUtils.ZOffsetFromCenter(init.Self, p, 1)
				)
			);
		}

		if (info.SequenceSummon == null)
			return;

		var animation = new Animation(init.Self.World, renderSprites.GetImage(init.Self));
		animation.PlayRepeating(RenderSprites.NormalizeSequence(animation, init.Self.GetDamageState(), info.SequenceSummon));

		renderSprites.Add(
			new(
				animation,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(init.Self, init.Self.Orientation))),
				() => this.IsTraitDisabled || this.summonTicker == 0,
				p => RenderUtils.ZOffsetFromCenter(init.Self, p, 1)
			)
		);
	}

	public void Enter()
	{
		this.population++;
		this.sacrificeTicker = this.info.Duration;
	}

	void ITick.Tick(Actor self)
	{
		if (this.sacrificeTicker > 0)
			this.sacrificeTicker--;

		if (this.summonTicker > 0)
		{
			if (--this.summonTicker != 0)
				return;

			var numSummons = this.population / this.info.Sacrifices;
			this.population -= numSummons * this.info.Sacrifices;

			self.World.AddFrameEndTask(
				_ =>
				{
					var td = new TypeDictionary { new OwnerInit(self.Owner) };

					for (var i = 0; i < numSummons; i++)
						this.Produce(self, self.World.Map.Rules.Actors[this.info.Summon], "produce", td, 0);
				}
			);
		}
		else if (this.population == this.info.Sacrifices)
			this.summonTicker = this.info.Duration;
	}
}

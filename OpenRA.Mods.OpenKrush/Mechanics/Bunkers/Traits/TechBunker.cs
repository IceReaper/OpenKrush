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

namespace OpenRA.Mods.OpenKrush.Mechanics.Bunkers.Traits
{
	using System;
	using System.Linq;
	using Common.Traits;
	using Common.Traits.Render;
	using Graphics;
	using OpenKrush.Traits.Production;
	using OpenRA.Graphics;
	using OpenRA.Traits;
	using Primitives;

	[Desc("Tech bunker mechanism.")]
	public class TechBunkerInfo : AdvancedProductionInfo, Requires<WithSpriteBodyInfo>, Requires<RenderSpritesInfo>
	{
		public const string LobbyOptionsCategory = "Tech Bunkers";

		[ActorReference]
		[Desc("Possible ejectable actors.")]
		public readonly string[] ContainableActors = null;

		[Desc("Amount of money. Use 0 to disable.")]
		public readonly int ContainableMoney = 5000;

		[Desc("Minimum amount of ticks till the bunker may unlock.")]
		public readonly int UnlockAfter = 15000;

		[Desc("The chance per tick the bunker may unlock in <1:x>.")]
		public readonly int UnlockChance = 1000;

		[Desc("Amount of ticks till the bunker locks again. Use -1 to disable.")]
		public readonly int LockAfter = 300;

		[Desc("The sound played when the bunker opens.")]
		public readonly string[] SoundOpen = null;

		[Desc("The sound played when the bunker closes.")]
		public readonly string[] SoundClose = null;

		[Desc("The distance to search for actors triggering bunker opening.")]
		public readonly WDist TriggerRadius = WDist.FromCells(3);

		[Desc("Idle sequence when the bunker is opened.")]
		public readonly string SequenceOpened = "idle-open";

		[Desc("Opening (and reversed closing) sequence.")]
		public readonly string SequenceOpening = "opening";

		[Desc("Locked effect sequence.")]
		public readonly string SequenceLocked = null;

		[Desc("Unlocked effect sequence.")]
		public readonly string SequenceUnlocked = null;

		public override object Create(ActorInitializer init)
		{
			return new TechBunker(init, this);
		}
	}

	public enum TechBunkerState
	{
		ClosedLocked,
		ClosedUnlocked,
		Opening,
		Opened,
		Closing
	}

	public class TechBunker : AdvancedProduction, ITick
	{
		private readonly TechBunkerInfo info;

		private readonly WithSpriteBody withSpriteBody;
		private readonly RenderSprites renderSprites;

		private readonly TechBunkerContains contains;
		private readonly TechBunkerUsage usage;
		private readonly TechBunkerUses uses;

		public TechBunkerState State = TechBunkerState.ClosedLocked;
		private int timer;

		public TechBunker(ActorInitializer init, TechBunkerInfo info)
			: base(init, info)
		{
			this.info = info;

			withSpriteBody = init.Self.Trait<WithSpriteBody>();
			renderSprites = init.Self.Trait<RenderSprites>();

			contains = init.Self.World.WorldActor.Trait<TechBunkerContains>();
			usage = init.Self.World.WorldActor.Trait<TechBunkerUsage>();
			uses = init.Self.World.WorldActor.Trait<TechBunkerUses>();

			AddAnimation(init.Self, info.SequenceLocked, () => State != TechBunkerState.ClosedLocked);
			AddAnimation(init.Self, info.SequenceUnlocked, () => State == TechBunkerState.ClosedLocked);
		}

		private void AddAnimation(Actor self, string sequence, Func<bool> hideWhen)
		{
			if (sequence == null)
				return;

			var overlay = new Animation(self.World, renderSprites.GetImage(self), hideWhen);
			overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), sequence));

			var anim = new AnimationWithOffset(
				overlay,
				() =>
				{
					var currentSequence = withSpriteBody.DefaultAnimation.CurrentSequence as OffsetsSpriteSequence;
					var sprite = withSpriteBody.DefaultAnimation.Image;

					if (currentSequence == null || !currentSequence.EmbeddedOffsets.ContainsKey(sprite) || currentSequence.EmbeddedOffsets[sprite] == null)
						return WVec.Zero;

					var point = currentSequence.EmbeddedOffsets[sprite].FirstOrDefault(p => p.Id == 0);

					return point != null ? new WVec(point.X * 32, point.Y * 32, 0) : WVec.Zero;
				},
				hideWhen,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			renderSprites.Add(anim);
		}

		void ITick.Tick(Actor self)
		{
			switch (State)
			{
				case TechBunkerState.ClosedLocked:
					if (timer++ >= info.UnlockAfter && self.World.SharedRandom.Next(0, info.UnlockChance) == 0)
					{
						State = TechBunkerState.ClosedUnlocked;
						timer = 0;
					}

					break;

				case TechBunkerState.ClosedUnlocked:
					if (usage.Usage != TechBunkerUsageType.Proximity)
						break;

					var nearbyActors = self.World.FindActorsInCircle(self.CenterPosition, info.TriggerRadius)
						.Where(actor => !actor.Owner.NonCombatant)
						.ToArray();

					if (!nearbyActors.Any())
						return;

					Use(self, nearbyActors[self.World.SharedRandom.Next(0, nearbyActors.Length - 1)].Owner);

					break;

				case TechBunkerState.Opened:
					if (uses.Uses == TechBunkerUsesType.Infinitely && info.LockAfter != -1 && timer++ >= info.LockAfter)
					{
						State = TechBunkerState.Closing;
						timer = 0;

						withSpriteBody.PlayCustomAnimationBackwards(
							self,
							info.SequenceOpening,
							() =>
							{
								State = TechBunkerState.ClosedLocked;
								withSpriteBody.CancelCustomAnimation(self);
							});

						if (info.SoundClose != null)
							Game.Sound.Play(SoundType.World, info.SoundClose.Random(self.World.SharedRandom), self.CenterPosition);
					}

					break;
			}
		}

		public void Use(Actor self, Player owner)
		{
			State = TechBunkerState.Opening;

			withSpriteBody.PlayCustomAnimation(
				self,
				info.SequenceOpening,
				() =>
				{
					State = TechBunkerState.Opened;
					withSpriteBody.PlayCustomAnimationRepeating(self, info.SequenceOpened);

					switch (contains.Contains)
					{
						case TechBunkerContainsType.Resources:
							EjectResources(owner);

							break;

						case TechBunkerContainsType.Both:
							if (self.World.SharedRandom.Next(0, info.ContainableActors.Length + 1) == 0)
								EjectResources(owner);
							else
								EjectUnit(self, owner);

							break;

						case TechBunkerContainsType.Units:
							EjectUnit(self, owner);

							break;
					}
				});

			if (info.SoundOpen != null)
				Game.Sound.Play(SoundType.World, info.SoundOpen.Random(self.World.SharedRandom), self.CenterPosition);
		}

		private void EjectResources(Player owner)
		{
			owner.PlayerActor.Trait<PlayerResources>().GiveCash(info.ContainableMoney);
		}

		private void EjectUnit(Actor self, Player owner)
		{
			var actor = info.ContainableActors[self.World.SharedRandom.Next(0, info.ContainableActors.Length)];

			var td = new TypeDictionary { new OwnerInit(owner) };

			Produce(self, self.World.Map.Rules.Actors[actor], "produce", td, 0);
		}
	}
}

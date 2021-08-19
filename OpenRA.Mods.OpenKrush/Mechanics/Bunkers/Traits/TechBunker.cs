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
	using Common.Traits;
	using Common.Traits.Render;
	using DataFromAssets.Graphics;
	using Graphics;
	using JetBrains.Annotations;
	using LobbyOptions;
	using OpenRA.Traits;
	using Primitives;
	using Production.Traits;
	using System;
	using System.Linq;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[Desc("Tech bunker mechanism.")]
	public class TechBunkerInfo : AdvancedProductionInfo, Requires<WithSpriteBodyInfo>, Requires<RenderSpritesInfo>
	{
		public const string LobbyOptionsCategory = "techbunker";

		[ActorReference]
		[Desc("Possible ejectable actors.")]
		public readonly string[] ContainableActors = Array.Empty<string>();

		[Desc("Amount of money. Use 0 to disable.")]
		public readonly int ContainableMoney = 5000;

		[Desc("Minimum amount of ticks till the bunker may unlock.")]
		public readonly int UnlockAfter = 15000;

		[Desc("The chance per tick the bunker may unlock in <1:x>.")]
		public readonly int UnlockChance = 1000;

		[Desc("Amount of ticks till the bunker locks again. Use -1 to disable.")]
		public readonly int LockAfter = 300;

		[Desc("The sound played when the bunker opens.")]
		public readonly string[] SoundOpen = Array.Empty<string>();

		[Desc("The sound played when the bunker closes.")]
		public readonly string[] SoundClose = Array.Empty<string>();

		[Desc("The distance to search for actors triggering bunker opening.")]
		public readonly WDist TriggerRadius = WDist.FromCells(3);

		[Desc("Idle sequence when the bunker is opened.")]
		public readonly string SequenceOpened = "idle-open";

		[Desc("Opening (and reversed closing) sequence.")]
		public readonly string SequenceOpening = "opening";

		[Desc("Locked effect sequence.")]
		public readonly string? SequenceLocked;

		[Desc("Unlocked effect sequence.")]
		public readonly string? SequenceUnlocked;

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

			this.withSpriteBody = init.Self.TraitOrDefault<WithSpriteBody>();
			this.renderSprites = init.Self.TraitOrDefault<RenderSprites>();

			this.contains = init.Self.World.WorldActor.TraitOrDefault<TechBunkerContains>();
			this.usage = init.Self.World.WorldActor.TraitOrDefault<TechBunkerUsage>();
			this.uses = init.Self.World.WorldActor.TraitOrDefault<TechBunkerUses>();

			if (info.SequenceLocked != null)
				this.AddAnimation(init.Self, info.SequenceLocked, () => this.State != TechBunkerState.ClosedLocked);

			if (info.SequenceUnlocked != null)
				this.AddAnimation(init.Self, info.SequenceUnlocked, () => this.State == TechBunkerState.ClosedLocked);
		}

		private void AddAnimation(Actor self, string sequence, Func<bool> hideWhen)
		{
			var overlay = new Animation(self.World, this.renderSprites.GetImage(self), hideWhen);
			overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), sequence));

			var anim = new AnimationWithOffset(
				overlay,
				() =>
				{
					var currentSequence = this.withSpriteBody.DefaultAnimation.CurrentSequence as OffsetsSpriteSequence;
					var sprite = this.withSpriteBody.DefaultAnimation.Image;

					if (currentSequence == null
						|| !currentSequence.EmbeddedOffsets.ContainsKey(sprite)
						|| currentSequence.EmbeddedOffsets.TryGetValue(sprite, out var offsets))
						return WVec.Zero;

					var point = offsets?.FirstOrDefault(p => p.Id == 0);

					return point != null ? new(point.X * 32, point.Y * 32, 0) : WVec.Zero;
				},
				hideWhen,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1)
			);

			this.renderSprites.Add(anim);
		}

		void ITick.Tick(Actor self)
		{
			switch (this.State)
			{
				case TechBunkerState.ClosedLocked:
					if (this.timer++ >= this.info.UnlockAfter && self.World.SharedRandom.Next(0, this.info.UnlockChance) == 0)
					{
						this.State = TechBunkerState.ClosedUnlocked;
						this.timer = 0;
					}

					break;

				case TechBunkerState.ClosedUnlocked:
					if (this.usage.Usage != TechBunkerUsageType.Proximity)
						break;

					var nearbyActors = self.World.FindActorsInCircle(self.CenterPosition, this.info.TriggerRadius)
						.Where(actor => !actor.Owner.NonCombatant)
						.ToArray();

					if (!nearbyActors.Any())
						return;

					this.Use(self, nearbyActors[self.World.SharedRandom.Next(0, nearbyActors.Length - 1)].Owner);

					break;

				case TechBunkerState.Opened:
					if (this.uses.Uses == TechBunkerUsesType.Infinitely && this.info.LockAfter != -1 && this.timer++ >= this.info.LockAfter)
					{
						this.State = TechBunkerState.Closing;
						this.timer = 0;

						this.withSpriteBody.PlayCustomAnimationBackwards(
							self,
							this.info.SequenceOpening,
							() =>
							{
								this.State = TechBunkerState.ClosedLocked;
								this.withSpriteBody.CancelCustomAnimation(self);
							}
						);

						Game.Sound.Play(SoundType.World, this.info.SoundClose.Random(self.World.SharedRandom), self.CenterPosition);
					}

					break;

				case TechBunkerState.Opening:
					break;

				case TechBunkerState.Closing:
					break;

				default:
					throw new ArgumentOutOfRangeException(Enum.GetName(this.State));
			}
		}

		public void Use(Actor self, Player owner)
		{
			this.State = TechBunkerState.Opening;

			this.withSpriteBody.PlayCustomAnimation(
				self,
				this.info.SequenceOpening,
				() =>
				{
					this.State = TechBunkerState.Opened;
					this.withSpriteBody.PlayCustomAnimationRepeating(self, this.info.SequenceOpened);

					switch (this.contains.Contains)
					{
						case TechBunkerContainsType.Resources:
							this.EjectResources(owner);

							break;

						case TechBunkerContainsType.Both:
							if (self.World.SharedRandom.Next(0, this.info.ContainableActors.Length + 1) == 0)
								this.EjectResources(owner);
							else
								this.EjectUnit(self, owner);

							break;

						case TechBunkerContainsType.Units:
							this.EjectUnit(self, owner);

							break;

						default:
							throw new ArgumentOutOfRangeException(Enum.GetName(this.contains.Contains));
					}
				}
			);

			Game.Sound.Play(SoundType.World, this.info.SoundOpen.Random(self.World.SharedRandom), self.CenterPosition);
		}

		private void EjectResources(Player owner)
		{
			owner.PlayerActor.TraitOrDefault<PlayerResources>().GiveCash(this.info.ContainableMoney);
		}

		private void EjectUnit(Actor self, Player owner)
		{
			var actor = this.info.ContainableActors[self.World.SharedRandom.Next(0, this.info.ContainableActors.Length)];

			var td = new TypeDictionary { new OwnerInit(owner) };

			this.Produce(self, self.World.Map.Rules.Actors[actor], "produce", td, 0);
		}
	}
}

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

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Bunkers
{
	[Desc("KKnD specific tech bunker implementation.")]
	class TechBunkerInfo : ITraitInfo, Requires<WithSpriteBodyInfo>
	{
		[ActorReference]
		[Desc("Possible ejectable actors.")]
		public readonly string[] ContainableActors = null;

		[Desc("Amount of money. Use 0 to disable.")]
		public readonly int ContainableMoney = 0;

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

		public readonly int MaximumDistance = 3;

		public object Create(ActorInitializer init) { return new TechBunker(init, this); }
	}

	internal enum TechBunkerState
	{
		ClosedLocked,
		ClosedUnlocked,
		Opening,
		Closing,
		Opened
	}

	class TechBunker : ITick
	{
		readonly TechBunkerInfo info;
		readonly WithSpriteBody wsb;
		int timer;
		TechBunkerState state;

		public TechBunker(ActorInitializer init, TechBunkerInfo info)
		{
			this.info = info;
			wsb = init.Self.Trait<WithSpriteBody>();
			timer = 0;
			state = TechBunkerState.ClosedLocked;

			var rs = init.Self.Trait<RenderSprites>();

			if (info.SequenceLocked != null)
			{
				var overlay = new Animation(init.World, rs.GetImage(init.Self), () => state != TechBunkerState.ClosedLocked);
				overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, init.Self.GetDamageState(), info.SequenceLocked));

				var anim = new AnimationWithOffset(overlay,
					() => WVec.Zero,
					() => state != TechBunkerState.ClosedLocked,
					p => RenderUtils.ZOffsetFromCenter(init.Self, p, 1));

				rs.Add(anim);
			}

			if (info.SequenceUnlocked != null)
			{
				var overlay = new Animation(init.World, rs.GetImage(init.Self), () => state != TechBunkerState.ClosedUnlocked);
				overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, init.Self.GetDamageState(), info.SequenceUnlocked));

				var anim = new AnimationWithOffset(overlay,
					() => WVec.Zero,
					() => state != TechBunkerState.ClosedUnlocked,
					p => RenderUtils.ZOffsetFromCenter(init.Self, p, 1));

				rs.Add(anim);
			}
		}

		void ITick.Tick(Actor self)
		{
			switch (state)
			{
				case TechBunkerState.ClosedLocked:
					if (timer++ >= info.UnlockAfter && self.World.SharedRandom.Next(0, info.UnlockChance) == 0)
					{
						state = TechBunkerState.ClosedUnlocked;
						timer = 0;
					}

					break;

				case TechBunkerState.ClosedUnlocked:
					var nearbyActors = self.World.FindActorsInCircle(self.CenterPosition, info.TriggerRadius).Where(actor => !actor.Owner.NonCombatant).ToArray();
					if (!nearbyActors.Any())
						return;

					var owner = nearbyActors[self.World.SharedRandom.Next(0, nearbyActors.Length - 1)].Owner;

					state = TechBunkerState.Opening;

					wsb.PlayCustomAnimation(self, info.SequenceOpening, () =>
					{
						state = TechBunkerState.Opened;
						wsb.PlayCustomAnimationRepeating(self, info.SequenceOpened);

						EjectContents(self, owner);
					});

					if (info.SoundOpen != null)
						Game.Sound.Play(SoundType.World, info.SoundOpen.Random(self.World.SharedRandom), self.CenterPosition);
					break;

				case TechBunkerState.Opened:
					if (self.World.WorldActor.Trait<TechBunkerBehavior>().Behavior == TechBunkerBehaviorType.Reusable && info.LockAfter != -1 && timer++ >= info.LockAfter)
					{
						state = TechBunkerState.Closing;
						timer = 0;

						wsb.PlayCustomAnimationBackwards(self, info.SequenceOpening, () =>
						{
							state = TechBunkerState.ClosedLocked;
							wsb.CancelCustomAnimation(self);
						});

						if (info.SoundClose != null)
							Game.Sound.Play(SoundType.World, info.SoundClose.Random(self.World.SharedRandom), self.CenterPosition);
					}

					break;
			}
		}

		void EjectContents(Actor self, Player owner)
		{
			if (info.ContainableMoney > 0 && self.World.SharedRandom.Next(0, info.ContainableActors.Length) == 0)
			{
				owner.PlayerActor.Trait<PlayerResources>().GiveCash(info.ContainableMoney);
				return;
			}

			self.World.AddFrameEndTask(w =>
			{
				var actor = info.ContainableActors[self.World.SharedRandom.Next(0, info.ContainableActors.Length)];

				var exit = SelectExit(self, self.World.Map.Rules.Actors[actor]).Info;
				var exitLocation = self.Location + exit.ExitCell;

				var td = new TypeDictionary
				{
					new OwnerInit(owner),
					new LocationInit(exitLocation),
					new CenterPositionInit(self.CenterPosition + exit.SpawnOffset),
					new FacingInit(exit.Facing)
				};

				var newUnit = self.World.CreateActor(actor, td);
				var move = newUnit.TraitOrDefault<IMove>();

				if (move != null)
				{
					if (exit.ExitDelay > 0)
						newUnit.QueueActivity(new Wait(exit.ExitDelay, false));

					newUnit.QueueActivity(new Move(newUnit, exitLocation));
					newUnit.QueueActivity(new AttackMoveActivity(newUnit, () => move.MoveTo(exitLocation, 1)));
				}

				foreach (var t in self.TraitsImplementing<INotifyProduction>())
					t.UnitProduced(self, newUnit, CPos.Zero);
			});
		}

		Exit SelectExit(Actor self, ActorInfo producee)
		{
			var mobileInfo = producee.TraitInfoOrDefault<MobileInfo>();

			var exit = self.Trait<Exit>();
			var spawn = self.World.Map.CellContaining(self.CenterPosition + exit.Info.SpawnOffset);

			for (var y = 1; y <= info.MaximumDistance; y++)
			for (var x = -y; x <= y; x++)
			{
				var candidate = new CVec(x, y);

				if (!mobileInfo.CanEnterCell(self.World, self, spawn + candidate))
					continue;

				var exitInfo = new ExitInfo();
				exitInfo.GetType().GetField("SpawnOffset").SetValue(exitInfo, exit.Info.SpawnOffset);
				exitInfo.GetType().GetField("ExitCell").SetValue(exitInfo, spawn - self.Location + candidate);
				exitInfo.GetType().GetField("Facing").SetValue(exitInfo, exit.Info.Facing);

				return new Exit(null, exitInfo);
			}

			return exit;
		}
	}
}

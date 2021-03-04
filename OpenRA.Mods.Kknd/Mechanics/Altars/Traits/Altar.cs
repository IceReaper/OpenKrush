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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Altars.Traits
{
	[Desc("Allow sacrificeable units to enter and spawn a new actor..")]
	public class AltarInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>
	{
		[Desc("The amount of units required for a sacrifice.")]
		public readonly int Sacrifices = 5;

		[Desc("Duration of the summoning.")]
		public readonly int SummonDelay = 50;

		[FieldLoader.RequireAttribute]
		[Desc("The unit which is granted upon sacrificing.")]
		public readonly string Summon = null;

		[Desc("Offset relative to the top-left cell of the building.")]
		public readonly CVec SpawnOffset = CVec.Zero;

		[Desc("Which direction the unit should face.")]
		public readonly int Facing = 0;

		[Desc("Sequence 1 to be played when sacrificing enough units.")]
		[SequenceReference]
		public readonly string SequenceEnter = null;

		[Desc("Sequence 2 to be played when sacrificing enough units.")]
		[SequenceReference]
		public readonly string SequenceSummon = null;

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		public readonly int MaximumDistance = 3;

		public override object Create(ActorInitializer init) { return new Altar(init.Self, this); }
	}

	public class Altar : ConditionalTrait<AltarInfo>, ITick
	{
		private int summonTicker;

		public int Population { get; private set; }

		public Altar(Actor self, AltarInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			if (Info.SequenceEnter != null)
			{
				var overlay1 = new Animation(self.World, rs.GetImage(self));
				overlay1.PlayRepeating(RenderSprites.NormalizeSequence(overlay1, self.GetDamageState(), Info.SequenceEnter));

				var anim1 = new AnimationWithOffset(overlay1,
					() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
					() => IsTraitDisabled || summonTicker == 0,
					p => RenderUtils.ZOffsetFromCenter(self, p, 1));

				rs.Add(anim1);
			}

			if (Info.SequenceSummon != null)
			{
				var overlay2 = new Animation(self.World, rs.GetImage(self));
				overlay2.PlayRepeating(RenderSprites.NormalizeSequence(overlay2, self.GetDamageState(), Info.SequenceSummon));

				var anim2 = new AnimationWithOffset(overlay2,
					() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
					() => IsTraitDisabled || summonTicker == 0,
					p => RenderUtils.ZOffsetFromCenter(self, p, 1));

				rs.Add(anim2);
			}
		}

		public void Enter()
		{
			Population++;

			if (Population == Info.Sacrifices)
				summonTicker = Info.SummonDelay;
		}

		void ITick.Tick(Actor self)
		{
			if (summonTicker == 0 || --summonTicker > 0)
				return;

			var numSummons = Population / Info.Sacrifices;
			Population -= numSummons * Info.Sacrifices;

			self.World.AddFrameEndTask(w =>
			{
				for (var i = 0; i < numSummons; i++)
					Summon(self);
			});
		}

		private void Summon(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				var actor = Info.Summon;

				var exit = SelectExit(self, self.World.Map.Rules.Actors[actor]).Info;
				var exitLocation = self.Location + exit.ExitCell;

				var td = new TypeDictionary
				{
					new OwnerInit(self.Owner),
					new LocationInit(exitLocation),
					new CenterPositionInit(self.CenterPosition + exit.SpawnOffset)
				};

				if (exit.Facing != null)
					td.Add(new FacingInit(exit.Facing.Value));

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

		private Exit SelectExit(Actor self, ActorInfo producee)
		{
			var mobileInfo = producee.TraitInfoOrDefault<MobileInfo>();

			var exit = self.Trait<Exit>();
			var spawn = self.World.Map.CellContaining(self.CenterPosition + exit.Info.SpawnOffset);

			for (var y = 1; y <= Info.MaximumDistance; y++)
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

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

using System.Drawing;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Veterancy
{
	[Desc("KKnD specific unit veterancy.")]
	public class VeterancyInfo : ITraitInfo
	{
		[Desc("Amount of experience (damage made on enemies) required for levelup.")]
		public readonly int[] Experience = { 1000, 2000 };

		[Desc("Veterancy level unit frame border color.")]
		public readonly Color[] Levels = { Color.FromArgb(255, 0, 165, 255), Color.FromArgb(255, 123, 0, 0) };

		[Desc("Percentual damage per veterancy level.")]
		public readonly int[] DamageRates = { 105, 110 };

		[Desc("Percentual inaccuracy per veterancy level.")]
		public readonly int[] InaccuracyRates = { 90, 70 };

		[Desc("Percentual range per veterancy level.")]
		public readonly int[] RangeRates = { 100, 100 };

		[Desc("Percentual reload times per veterancy level.")]
		public readonly int[] ReloadRates = { 95, 90 };

		[Desc("Percentual speed per veterancy level.")]
		public readonly int[] SpeedRates = { 100, 100 };

		[Desc("Self heal per veterancy level.")]
		public readonly int[] HealRates = { };

		[Desc("Delay in ticks between healing.")]
		public readonly int HealDelay = 1;

		[Desc("Apply the selfhealing using these damagetypes.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		public object Create(ActorInitializer init) { return new Veterancy(init, this); }
	}

	public class Veterancy : INotifyAppliedDamage, IDamageModifier, IInaccuracyModifier, IRangeModifier, IReloadModifier, ISpeedModifier, ITick
	{
		private readonly VeterancyInfo info;
		private readonly Health health;

		private int experience;
		public int Level { get; private set; }

		public Veterancy(ActorInitializer init, VeterancyInfo info)
		{
			this.info = info;
			health = init.Self.TraitOrDefault<Health>();
		}

		void INotifyAppliedDamage.AppliedDamage(Actor self, Actor damaged, AttackInfo e)
		{
			if (Level == info.Levels.Length)
				return;

			if (self.Owner.Stances[damaged.Owner] != Stance.Enemy)
				return;

			experience += e.Damage.Value;

			if (experience < info.Experience[Level])
				return;

			experience -= info.Experience[Level];
			Level++;
		}

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			return Level == 0 ? 100 : info.DamageRates[Level - 1];
		}

		int IInaccuracyModifier.GetInaccuracyModifier()
		{
			return Level == 0 ? 100 : info.InaccuracyRates[Level - 1];
		}

		int IRangeModifier.GetRangeModifier()
		{
			return Level == 0 ? 100 : info.RangeRates[Level - 1];
		}

		int IReloadModifier.GetReloadModifier()
		{
			return Level == 0 ? 100 : info.ReloadRates[Level - 1];
		}

		int ISpeedModifier.GetSpeedModifier()
		{
			return Level == 0 ? 100 : info.SpeedRates[Level - 1];
		}

		void ITick.Tick(Actor self)
		{
			if (info.HealRates.Length == 0 || Level == 0)
				return;

			if (self.CurrentActivity == null && self.World.WorldTick % info.HealDelay == 0)
				health.InflictDamage(self, self, new Damage(-info.HealRates[Level - 1], info.DamageTypes), true);
		}
	}
}

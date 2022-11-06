#region Copyright & License Information

/*
 * Copyright 2007-2022 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Mechanics.Veterancy.Traits;

using Common.Traits;
using JetBrains.Annotations;
using OpenRA.Traits;
using Primitives;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("Unit veterancy.")]
public class VeterancyInfo : TraitInfo
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
	public readonly int[] HealRates = Array.Empty<int>();

	[Desc("Delay in ticks between healing.")]
	public readonly int HealDelay = 1;

	[Desc("Apply the selfhealing using these damagetypes.")]
	public readonly BitSet<DamageType> DamageTypes;

	public override object Create(ActorInitializer init)
	{
		return new Veterancy(init, this);
	}
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
		this.health = init.Self.TraitOrDefault<Health>();
	}

	void INotifyAppliedDamage.AppliedDamage(Actor self, Actor damaged, AttackInfo e)
	{
		if (this.Level == this.info.Levels.Length)
			return;

		if (self.Owner.RelationshipWith(damaged.Owner) != PlayerRelationship.Enemy)
			return;

		this.experience += e.Damage.Value;

		if (this.experience < this.info.Experience[this.Level])
			return;

		this.experience -= this.info.Experience[this.Level];
		this.Level++;
	}

	int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
	{
		return this.Level == 0 ? 100 : this.info.DamageRates[this.Level - 1];
	}

	int IInaccuracyModifier.GetInaccuracyModifier()
	{
		return this.Level == 0 ? 100 : this.info.InaccuracyRates[this.Level - 1];
	}

	int IRangeModifier.GetRangeModifier()
	{
		return this.Level == 0 ? 100 : this.info.RangeRates[this.Level - 1];
	}

	int IReloadModifier.GetReloadModifier()
	{
		return this.Level == 0 ? 100 : this.info.ReloadRates[this.Level - 1];
	}

	int ISpeedModifier.GetSpeedModifier()
	{
		return this.Level == 0 ? 100 : this.info.SpeedRates[this.Level - 1];
	}

	void ITick.Tick(Actor self)
	{
		if (this.info.HealRates.Length == 0 || this.Level == 0)
			return;

		if (self.CurrentActivity == null && self.World.WorldTick % this.info.HealDelay == 0)
			this.health.InflictDamage(self, self, new(-this.info.HealRates[this.Level - 1], this.info.DamageTypes), true);
	}
}

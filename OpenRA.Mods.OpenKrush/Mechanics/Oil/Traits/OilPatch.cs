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

namespace OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits;

using GameRules;
using JetBrains.Annotations;
using LobbyOptions;
using OpenRA.Traits;
using Primitives;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("Oilpatch implementation.")]
public class OilPatchInfo : TraitInfo, IRulesetLoaded, IHealthInfo
{
	public const string LobbyOptionsCategory = "oilpatch";

	[Desc("How many oil will be burned per tick.")]
	public readonly int BurnAmount = 5;

	[Desc("Amount of oil on spawn. Use -1 for infinite")]
	public readonly int Amount;

	[Desc("Amount of oil for a full oilpatch.")]
	public readonly int FullAmount = 100000;

	[GrantedConditionReference]
	[Desc("Condition to grant while this actor is burning.")]
	public readonly string Condition = "Burning";

	[WeaponReference]
	[Desc("Has to be defined in weapons.yaml as well.")]
	public readonly string Weapon = "oilburn";

	public int MaxHP => this.FullAmount;

	public WeaponInfo? WeaponInfo { get; private set; }

	public override object Create(ActorInitializer init)
	{
		return new OilPatch(init, this);
	}

	public void RulesetLoaded(Ruleset rules, ActorInfo ai)
	{
		if (string.IsNullOrEmpty(this.Weapon))
			return;

		var weaponToLower = this.Weapon.ToLowerInvariant();

		if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
			throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

		this.WeaponInfo = weapon;
	}
}

public class OilPatch : IHealth, ITick, IHaveOil
{
	public readonly OilPatchInfo Info;

	private int resources;

	public DamageState DamageState => DamageState.Undamaged;
	public int HP => this.resources == -1 ? this.Info.FullAmount : Math.Min(this.resources, this.Info.FullAmount);
	public int MaxHP => this.Info.FullAmount;
	public int DisplayHP => this.HP;
	public bool IsDead => this.resources == 0;

	private int token = Actor.InvalidConditionToken;

	private readonly int burnTotal;
	private int burnLeft;

	public Actor? Drillrig;

	public OilPatch(IActorInitializer init, OilPatchInfo info)
	{
		this.Info = info;
		this.resources = info.Amount == 0 ? init.World.WorldActor.TraitOrDefault<OilAmount>().Amount : info.Amount;
		this.burnTotal = this.Info.FullAmount * init.World.WorldActor.TraitOrDefault<OilBurn>().Amount / 100;
	}

	public int Current => this.resources == -1 ? this.Info.FullAmount : this.resources;
	public int Maximum => this.Info.FullAmount;

	public int Pull(int amount)
	{
		if (this.resources == -1)
			return amount;

		var pullAmount = Math.Min(amount, this.Current);
		this.resources -= pullAmount;

		return pullAmount;
	}

	public void Push(int amount)
	{
		if (this.resources != -1)
			this.resources += amount;
	}

	void ITick.Tick(Actor self)
	{
		if (this.token != Actor.InvalidConditionToken)
		{
			if (this.Drillrig != null)
			{
				this.token = self.RevokeCondition(this.token);
				this.burnLeft = 0;
			}
			else
			{
				var damage = Math.Min(this.Info.BurnAmount, this.burnLeft);
				this.burnLeft -= damage;

				if (this.resources > 0)
					this.resources -= Math.Min(this.resources, damage);

				if (this.burnLeft == 0)
					this.token = self.RevokeCondition(this.token);

				this.Info.WeaponInfo?.Impact(Target.FromPos(self.CenterPosition), self);
			}
		}

		if (this.resources == 0)
			self.Dispose();
	}

	void IHealth.InflictDamage(Actor self, Actor attacker, Damage damage, bool ignoreModifiers)
	{
		if (this.Drillrig != null || this.burnTotal == 0 || this.Info.BurnAmount == 0 || damage.Value <= 0 || self.Equals(attacker))
			return;

		this.burnLeft = this.burnTotal;

		if (!string.IsNullOrEmpty(this.Info.Condition) && this.token == Actor.InvalidConditionToken)
			this.token = self.GrantCondition(this.Info.Condition);
	}

	void IHealth.Kill(Actor self, Actor attacker, BitSet<DamageType> damageTypes)
	{
		this.resources = 0;
	}
}

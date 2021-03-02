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
using OpenRA.GameRules;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Resources
{
	[Desc("KKnD specific oilpatch implementation.")]
	class OilpatchInfo : TraitInfo, IRulesetLoaded, IHealthInfo
	{
		[Desc("How many oil will be burned per tick.")]
		public readonly int BurnAmount = 5;

		[Desc("Amount of oil on spawn. Use -1 for infinite")]
		public readonly int Amount = 0;

		[Desc("Amount of oil for a full oilpatch.")]
		public readonly int FullAmount = 100000;

		[GrantedConditionReference]
		[Desc("Condition to grant while this actor is burning.")]
		public readonly string Condition = "Burning";

		[WeaponReference]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = "oilburn";

		public int MaxHP { get { return FullAmount; } }

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new Oilpatch(init, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!string.IsNullOrEmpty(Weapon))
			{
				WeaponInfo weapon;
				var weaponToLower = Weapon.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(weaponToLower, out weapon))
					throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));
				WeaponInfo = weapon;
			}
		}
	}

	class Oilpatch : IHealth, ITick, IHaveOil
	{
		private readonly OilpatchInfo info;

		private int resources;

		public DamageState DamageState { get { return DamageState.Undamaged; } }
		public int HP { get { return resources == -1 ? info.FullAmount : Math.Min(resources, info.FullAmount); } }
		public int MaxHP { get { return info.FullAmount; } }
		public int DisplayHP { get { return HP; } }
		public bool IsDead { get { return resources == 0; } }

		private int token = Actor.InvalidConditionToken;

		private int burnTotal;
		private int burnLeft;

		public Oilpatch(ActorInitializer init, OilpatchInfo info)
		{
			this.info = info;
			resources = info.Amount == 0 ? init.World.WorldActor.Trait<OilAmount>().Amount : info.Amount;
			burnTotal = this.info.FullAmount * init.World.WorldActor.Trait<OilpatchBurn>().Amount / 100;
		}

		public int Current { get { return resources == -1 ? info.FullAmount : resources; } }
		public int Maximum { get { return info.FullAmount; } }

		public int Pull(int amount)
		{
			if (resources == -1)
				return amount;

			var pullAmount = Math.Min(amount, Current);
			resources -= pullAmount;
			return pullAmount;
		}

		public int Push(int amount)
		{
			if (resources != -1)
				resources += amount;

			return 0;
		}

		void ITick.Tick(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
			{
				if (!self.IsInWorld)
				{
					self.RevokeCondition(token);
					token = Actor.InvalidConditionToken;
					burnLeft = 0;
				}
				else
				{
					var damage = Math.Min(info.BurnAmount, burnLeft);
					burnLeft -= damage;

					if (resources > 0)
						resources -= Math.Min(resources, damage);

					if (burnLeft == 0)
					{
						self.RevokeCondition(token);
						token = Actor.InvalidConditionToken;
					}

					info.WeaponInfo.Impact(Target.FromPos(self.CenterPosition), self);
				}
			}

			if (resources == 0)
				self.Dispose();
		}

		void IHealth.InflictDamage(Actor self, Actor attacker, Damage damage, bool ignoreModifiers)
		{
			if (burnTotal == 0 || info.BurnAmount == 0 || damage.Value <= 0 || attacker == self)
				return;

			burnLeft = burnTotal;

			if (!string.IsNullOrEmpty(info.Condition) && token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);
		}

		void IHealth.Kill(Actor self, Actor attacker, BitSet<DamageType> damageTypes)
		{
			resources = 0;
		}
	}
}

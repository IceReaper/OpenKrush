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

namespace OpenRA.Mods.OpenKrush.Mechanics.AttackNotifications.Traits;

using Common.Traits;
using JetBrains.Annotations;
using OpenRA.Traits;
using Primitives;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("Attack notifier which supports per actor notifications.")]
public class AdvancedAttackNotifierInfo : TraitInfo
{
	[Desc("Minimum duration (in seconds) between notification events.")]
	public readonly int NotifyInterval = 30;

	public readonly Color RadarPingColor = Color.Red;

	[Desc("Length of time (in ticks) to display a location ping in the minimap.")]
	public readonly int RadarPingDuration = 10 * 25;

	public override object Create(ActorInitializer init)
	{
		return new AdvancedAttackNotifier(this);
	}
}

public class AdvancedAttackNotifier : INotifyDamage, INotifyCreated
{
	private readonly AdvancedAttackNotifierInfo info;
	private readonly Dictionary<string, int> lastAttackTimes = new();
	private RadarPings? radarPings;

	public AdvancedAttackNotifier(AdvancedAttackNotifierInfo info)
	{
		this.info = info;
	}

	void INotifyCreated.Created(Actor self)
	{
		this.radarPings = self.World.WorldActor.TraitOrDefault<RadarPings>();
	}

	void INotifyDamage.Damaged(Actor self, AttackInfo attackInfo)
	{
		if (attackInfo.Attacker == null)
			return;

		if (attackInfo.Attacker.Owner == self.Owner)
			return;

		if (attackInfo.Attacker.Equals(self.World.WorldActor))
			return;

		if (attackInfo.Damage.Value == 0)
			return;

		var attackNotification = self.TraitOrDefault<AttackNotification>();

		if (attackNotification == null)
			return;

		var notification = attackNotification.Info.Notifications[(attackNotification.Veterancy?.Level ?? 0) % attackNotification.Info.Notifications.Length];

		if (!this.lastAttackTimes.ContainsKey(notification))
			this.lastAttackTimes.Add(notification, self.World.WorldTick - this.info.NotifyInterval * 25);

		if (self.World.WorldTick - this.lastAttackTimes[notification] >= this.info.NotifyInterval * 25)
		{
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", notification, self.Owner.Faction.InternalName);

			if (attackNotification.Info.RadarPings)
			{
				this.radarPings?.Add(
					() => self.Owner.IsAlliedWith(self.World.RenderPlayer),
					self.CenterPosition,
					this.info.RadarPingColor,
					this.info.RadarPingDuration
				);
			}
		}

		this.lastAttackTimes[notification] = self.World.WorldTick;
	}
}

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

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenKrush.Traits.AttackNotifications
{
	[Desc("Attack notifier which supports per actor notifications.")]
	public class AdvancedAttackNotifierInfo : TraitInfo
	{
		[Desc("Minimum duration (in seconds) between notification events.")]
		public readonly int NotifyInterval = 30;

		public readonly Color RadarPingColor = Color.Red;

		[Desc("Length of time (in ticks) to display a location ping in the minimap.")]
		public readonly int RadarPingDuration = 10 * 25;

		public override object Create(ActorInitializer init) { return new AdvancedAttackNotifier(init.Self, this); }
	}

	public class AdvancedAttackNotifier : INotifyDamage
	{
		readonly RadarPings radarPings;
		readonly AdvancedAttackNotifierInfo info;

		Dictionary<string, int> lastAttackTimes = new Dictionary<string, int>();

		public AdvancedAttackNotifier(Actor self, AdvancedAttackNotifierInfo info)
		{
			radarPings = self.World.WorldActor.Trait<RadarPings>();
			this.info = info;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Attacker == null)
				return;

			if (e.Attacker.Owner == self.Owner)
				return;

			if (e.Attacker == self.World.WorldActor)
				return;

			if (e.Damage.Value == 0)
				return;

			var ani = self.Info.TraitInfoOrDefault<AttackNotificationInfo>();

			if (ani == null)
				return;

			var veterancy = self.TraitOrDefault<Veterancy.Veterancy>();
			var notification = ani.Notifications[(veterancy == null ? 0 : veterancy.Level) % ani.Notifications.Length];

			if (!lastAttackTimes.ContainsKey(notification))
				lastAttackTimes.Add(notification, self.World.WorldTick - info.NotifyInterval * 25);

			if (self.World.WorldTick - lastAttackTimes[notification] >= info.NotifyInterval * 25)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", notification, self.Owner.Faction.InternalName);

				if (ani.RadarPings)
					radarPings.Add(() => self.Owner.IsAlliedWith(self.World.RenderPlayer), self.CenterPosition, info.RadarPingColor, info.RadarPingDuration);
			}

			lastAttackTimes[notification] = self.World.WorldTick;
		}
	}
}

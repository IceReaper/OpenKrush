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

namespace OpenRA.Mods.OpenKrush.Mechanics.AttackNotifications.Traits
{
	using JetBrains.Annotations;
	using OpenRA.Traits;
	using System;
	using Veterancy.Traits;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[Desc("Specifies the notification for the AdvancedAttackNotifier.")]
	public class AttackNotificationInfo : TraitInfo
	{
		[Desc("The audio notification type to play.")]
		[FieldLoader.RequireAttribute]
		public string[] Notifications = Array.Empty<string>();

		public bool RadarPings = true;

		public override object Create(ActorInitializer init)
		{
			return new AttackNotification(this);
		}
	}

	public class AttackNotification : INotifyCreated
	{
		public readonly AttackNotificationInfo Info;
		public Veterancy? Veterancy;

		public AttackNotification(AttackNotificationInfo info)
		{
			this.Info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			this.Veterancy = self.TraitOrDefault<Veterancy>();
		}
	}
}

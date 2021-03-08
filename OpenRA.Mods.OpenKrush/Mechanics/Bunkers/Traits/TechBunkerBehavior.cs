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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenKrush.Mechanics.Bunkers.Traits
{
	public enum TechBunkerBehaviorType
	{
		SingleUsage,
		Reusable
	}

	[Desc("Tech bunker usage behavior.")]
	public class TechBunkerBehaviorInfo : TraitInfo, ILobbyOptions
	{
		public const string Id = "TechBunkerBehavior";

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyOption(
				Id,
				"Bunker Behavior",
				"TechBunker behavior.",
				true,
				0,
				new ReadOnlyDictionary<string, string>(new Dictionary<TechBunkerBehaviorType, string>
				{
					{ TechBunkerBehaviorType.SingleUsage, "Single-Usage" },
					{ TechBunkerBehaviorType.Reusable, "Reusable" }
				}.ToDictionary(e => e.Key.ToString(), e => e.Value)),
				TechBunkerBehaviorType.Reusable.ToString(),
				false);
		}

		public override object Create(ActorInitializer init)
		{
			return new TechBunkerBehavior();
		}
	}

	public class TechBunkerBehavior : INotifyCreated
	{
		public TechBunkerBehaviorType Behavior { get; private set; }

		void INotifyCreated.Created(Actor self)
		{
			Behavior = (TechBunkerBehaviorType)Enum.Parse(typeof(TechBunkerBehaviorType),
				self.World.LobbyInfo.GlobalSettings.OptionOrDefault(TechBunkerBehaviorInfo.Id, TechBunkerBehaviorType.Reusable.ToString()));
		}
	}
}

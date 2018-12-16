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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Bunkers
{
	[Desc("Selectable oilpatch oil amount in lobby.")]
	public class BunkerSettingsInfo : ITraitInfo, ILobbyOptions
	{
		public readonly string[] Values = {"Disabled", "Single Usage", "Reusable"};

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var values = new Dictionary<string, string>();

			foreach (var value in Values)
				values.Add(value, value);

			yield return new LobbyOption("bunkers", "Bunkers", "TechBunker behavior.", true, 0, new ReadOnlyDictionary<string, string>(values), "Reusable", false);
		}

		public object Create(ActorInitializer init) { return new BunkerSettings(); }
	}

	public class BunkerSettings : INotifyCreated
	{
		public bool Enabled { get; set; }
		public bool Reusable { get; set; }

		void INotifyCreated.Created(Actor self)
		{
			Enabled = self.World.LobbyInfo.GlobalSettings.OptionOrDefault("bunkers", "Reusable") != "Disabled";
			Reusable = self.World.LobbyInfo.GlobalSettings.OptionOrDefault("bunkers", "Reusable") == "Reusable";
		}
	}
}

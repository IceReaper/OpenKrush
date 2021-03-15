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

namespace OpenRA.Mods.OpenKrush.Mechanics.Bunkers.LobbyOptions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using OpenRA.Traits;
	using Traits;

	public enum TechBunkerContainsType
	{
		Resources,
		Units,
		Both
	}

	[Desc("What a TechBunker may contain.")]
	public class TechBunkerContainsInfo : TraitInfo, ILobbyOptions
	{
		public const string Id = "TechBunkerContains";
		public const TechBunkerContainsType Default = TechBunkerContainsType.Units;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyOption(
				TechBunkerContainsInfo.Id,
				"Contains",
				"What a TechBunker may contain.",
				true,
				0,
				new ReadOnlyDictionary<string, string>(
					new Dictionary<TechBunkerContainsType, string>
					{
						{ TechBunkerContainsType.Resources, "Resources" },
						{ TechBunkerContainsType.Units, "Units" },
						{ TechBunkerContainsType.Both, "Both" }
					}.ToDictionary(e => e.Key.ToString(), e => e.Value)),
				TechBunkerContainsInfo.Default.ToString(),
				false,
				TechBunkerInfo.LobbyOptionsCategory);
		}

		public override object Create(ActorInitializer init)
		{
			return new TechBunkerContains();
		}
	}

	public class TechBunkerContains : INotifyCreated
	{
		public TechBunkerContainsType Contains { get; private set; }

		void INotifyCreated.Created(Actor self)
		{
			Contains = (TechBunkerContainsType)Enum.Parse(
				typeof(TechBunkerContainsType),
				self.World.LobbyInfo.GlobalSettings.OptionOrDefault(TechBunkerContainsInfo.Id, TechBunkerContainsInfo.Default.ToString()));
		}
	}
}

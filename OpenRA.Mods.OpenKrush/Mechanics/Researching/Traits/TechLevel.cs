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

namespace OpenRA.Mods.OpenKrush.Mechanics.Researching.Traits
{
	using System.Collections.Generic;
	using OpenRA.Traits;

	[Desc("Selectable max tech level in lobby.")]
	public class TechLevelInfo : TraitInfo, ILobbyOptions
	{
		public const string Id = "TechLevel";

		public readonly int TechLevels = 5;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var values = new Dictionary<string, string>();

			for (var i = 1; i <= TechLevels; i++)
				values.Add(i.ToString(), i.ToString());

			yield return new LobbyOption(
				TechLevelInfo.Id,
				"Tech level",
				"Maximum tech level.",
				true,
				0,
				new ReadOnlyDictionary<string, string>(values),
				TechLevels.ToString(),
				false);
		}

		public override object Create(ActorInitializer init)
		{
			return new TechLevel(this);
		}
	}

	public class TechLevel : INotifyCreated
	{
		private readonly TechLevelInfo info;
		public int TechLevels { get; private set; }

		public TechLevel(TechLevelInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			TechLevels = int.Parse(self.World.LobbyInfo.GlobalSettings.OptionOrDefault(TechLevelInfo.Id, info.TechLevels.ToString()));
		}
	}
}

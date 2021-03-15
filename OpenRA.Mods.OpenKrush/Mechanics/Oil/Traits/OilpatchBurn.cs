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
using OpenRA.Traits;

namespace OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits
{
	[Desc("Selectable oil burn behavior in lobby.")]
	public class OilpatchBurnInfo : TraitInfo, ILobbyOptions
	{
		public const string Id = "OilBurn";

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var values = new Dictionary<string, string>();

			for (var i = 0; i <= 100; i += 20)
				values.Add(i.ToString(), $"{i}%");

			yield return new LobbyOption(
				Id,
				"Burn",
				"Percent amount of oil to burn when ignited.",
				true,
				0,
				new ReadOnlyDictionary<string, string>(values),
				"0",
				false,
				OilpatchInfo.LobbyOptionsCategory);
		}

		public override object Create(ActorInitializer init) { return new OilpatchBurn(); }
	}

	public class OilpatchBurn : INotifyCreated
	{
		public int Amount { get; set; }

		void INotifyCreated.Created(Actor self)
		{
			Amount = int.Parse(self.World.LobbyInfo.GlobalSettings.OptionOrDefault(OilpatchBurnInfo.Id, "0"));
		}
	}
}

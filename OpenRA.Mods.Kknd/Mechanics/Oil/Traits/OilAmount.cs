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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Oil.Traits
{
	[Desc("Selectable oilpatch oil amount in lobby.")]
	public class OilAmountInfo : TraitInfo, ILobbyOptions
	{
		public readonly int[] OilAmounts = { 25000, 50000, 75000, 100000, -1 };
		public readonly string[] OilAmountNames = { "Scarce", "Normal", "Abundant", "Maximum", "Infinite" };

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var values = new Dictionary<string, string>();

			for (var i = 0; i < OilAmountNames.Length; i++)
				values.Add(OilAmounts[i].ToString(), OilAmountNames[i]);

			var standard = OilAmounts[OilAmountNames.IndexOf("Normal")];
			yield return new LobbyOption(
				"oilpatches",
				"Oilpatches",
				"Amount of oil every oilpatch contains.",
				true,
				0,
				new ReadOnlyDictionary<string, string>(values),
				standard.ToString(),
				false);
		}

		public override object Create(ActorInitializer init) { return new OilAmount(this); }
	}

	public class OilAmount : INotifyCreated
	{
		private readonly OilAmountInfo info;
		public int Amount { get; set; }

		public OilAmount(OilAmountInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var standard = info.OilAmounts[info.OilAmountNames.IndexOf("Normal")];
			Amount = int.Parse(self.World.LobbyInfo.GlobalSettings.OptionOrDefault("oilpatches", standard.ToString()));
		}
	}
}

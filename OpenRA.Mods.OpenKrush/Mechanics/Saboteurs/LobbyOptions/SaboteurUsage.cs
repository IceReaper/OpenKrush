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

namespace OpenRA.Mods.OpenKrush.Mechanics.Saboteurs.LobbyOptions
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using OpenRA.Traits;

	public enum SaboteurUsageType
	{
		Destroy,
		Conquer
	}

	[Desc("What happens when a saboteur conquers a building.")]
	public class SaboteurUsageInfo : TraitInfo, ILobbyOptions
	{
		public const string LobbyOptionsCategory = "saboteur";

		public const string Id = "SaboteurUsage";
		public const SaboteurUsageType Default = SaboteurUsageType.Conquer;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview mapPreview)
		{
			yield return new LobbyOption(
				SaboteurUsageInfo.Id,
				"Usage",
				"What happens when a saboteur conquers a building.",
				true,
				0,
				new ReadOnlyDictionary<string, string>(
					new Dictionary<SaboteurUsageType, string>
					{
						{ SaboteurUsageType.Destroy, "Destroy" }, { SaboteurUsageType.Conquer, "Conquer" }
					}.ToDictionary(e => e.Key.ToString(), e => e.Value)),
				SaboteurUsageInfo.Default.ToString(),
				false,
				SaboteurUsageInfo.LobbyOptionsCategory);
		}

		public override object Create(ActorInitializer init)
		{
			return new SaboteurUsage();
		}
	}

	public class SaboteurUsage : INotifyCreated
	{
		public SaboteurUsageType Usage { get; private set; }

		void INotifyCreated.Created(Actor self)
		{
			Usage = (SaboteurUsageType)Enum.Parse(
				typeof(SaboteurUsageType),
				self.World.LobbyInfo.GlobalSettings.OptionOrDefault(SaboteurUsageInfo.Id, SaboteurUsageInfo.Default.ToString()));
		}
	}
}

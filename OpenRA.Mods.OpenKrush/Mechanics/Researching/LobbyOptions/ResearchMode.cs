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

namespace OpenRA.Mods.OpenKrush.Mechanics.Researching.LobbyOptions
{
	using JetBrains.Annotations;
	using OpenRA.Traits;
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;

	public enum ResearchModeType
	{
		FullLevel,
		SingleTech
	}

	[UsedImplicitly]
	[Desc("How the research system should work.")]
	public class ResearchModeInfo : TraitInfo, ILobbyOptions
	{
		public const string Id = "ResearchMode";
		public const ResearchModeType Default = ResearchModeType.FullLevel;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview mapPreview)
		{
			yield return new LobbyOption(
				ResearchModeInfo.Id,
				"Mode",
				"Wether to research a full tech level or a single technology.",
				true,
				0,
				new ReadOnlyDictionary<string, string>(
					new Dictionary<ResearchModeType, string> { { ResearchModeType.FullLevel, "Full Level" }, { ResearchModeType.SingleTech, "Single Tech" } }
						.ToDictionary(e => e.Key.ToString(), e => e.Value)
				),
				ResearchModeInfo.Default.ToString(),
				false,
				ResearchUtils.LobbyOptionsCategory
			);
		}

		public override object Create(ActorInitializer init)
		{
			return new ResearchMode();
		}
	}

	public class ResearchMode : INotifyCreated
	{
		public ResearchModeType Mode { get; private set; }

		void INotifyCreated.Created(Actor self)
		{
			this.Mode = (ResearchModeType)Enum.Parse(
				typeof(ResearchModeType),
				self.World.LobbyInfo.GlobalSettings.OptionOrDefault(ResearchModeInfo.Id, ResearchModeInfo.Default.ToString())
			);
		}
	}
}

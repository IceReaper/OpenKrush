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

namespace OpenRA.Mods.OpenKrush.Mechanics.Bunkers.LobbyOptions;

using JetBrains.Annotations;
using OpenRA.Traits;
using System.Collections.ObjectModel;
using Traits;

public enum TechBunkerUsageType
{
	Proximity,
	Technician
}

[UsedImplicitly]
[Desc("How a TechBunker can be opened.")]
public class TechBunkerUsageInfo : TraitInfo, ILobbyOptions
{
	public const string Id = "TechBunkerUsage";
	public const TechBunkerUsageType Default = TechBunkerUsageType.Proximity;

	IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview mapPreview)
	{
		yield return new(
			TechBunkerUsageInfo.Id,
			"Usage",
			"How a TechBunker can be opened.",
			true,
			0,
			new ReadOnlyDictionary<string, string>(
				new Dictionary<TechBunkerUsageType, string> { { TechBunkerUsageType.Proximity, "Proximity" }, { TechBunkerUsageType.Technician, "Technician" } }
					.ToDictionary(e => e.Key.ToString(), e => e.Value)
			),
			TechBunkerUsageInfo.Default.ToString(),
			false,
			TechBunkerInfo.LobbyOptionsCategory
		);
	}

	public override object Create(ActorInitializer init)
	{
		return new TechBunkerUsage();
	}
}

public class TechBunkerUsage : INotifyCreated
{
	public TechBunkerUsageType Usage { get; private set; }

	void INotifyCreated.Created(Actor self)
	{
		this.Usage = (TechBunkerUsageType)Enum.Parse(
			typeof(TechBunkerUsageType),
			self.World.LobbyInfo.GlobalSettings.OptionOrDefault(TechBunkerUsageInfo.Id, TechBunkerUsageInfo.Default.ToString())
		);
	}
}

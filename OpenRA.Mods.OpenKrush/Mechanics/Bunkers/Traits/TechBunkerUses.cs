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
	public enum TechBunkerUsesType
	{
		Once,
		Infinitely
	}

	[Desc("How many times a TechBunker can be used.")]
	public class TechBunkerUsesInfo : TraitInfo, ILobbyOptions
	{
		public const string Id = "TechBunkerUses";
		public const TechBunkerUsesType Default = TechBunkerUsesType.Infinitely;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyOption(
				Id,
				"Uses",
				"How many times a TechBunker can be used.",
				true,
				0,
				new ReadOnlyDictionary<string, string>(new Dictionary<TechBunkerUsesType, string>
				{
					{ TechBunkerUsesType.Once, "Once" },
					{ TechBunkerUsesType.Infinitely, "Infinitely" }
				}.ToDictionary(e => e.Key.ToString(), e => e.Value)),
				Default.ToString(),
				false,
				TechBunkerInfo.LobbyOptionsCategory);
		}

		public override object Create(ActorInitializer init)
		{
			return new TechBunkerUses();
		}
	}

	public class TechBunkerUses : INotifyCreated
	{
		public TechBunkerUsesType Uses { get; private set; }

		void INotifyCreated.Created(Actor self)
		{
			Uses = (TechBunkerUsesType)Enum.Parse(typeof(TechBunkerUsesType),
				self.World.LobbyInfo.GlobalSettings.OptionOrDefault(TechBunkerUsesInfo.Id, TechBunkerUsesInfo.Default.ToString()));
		}
	}
}

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

namespace OpenRA.Mods.OpenKrush.Mechanics.Bunkers.Traits
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Common.Traits;
	using OpenRA.Graphics;
	using OpenRA.Traits;

	public enum TechBunkerAmountType
	{
		None,
		One,
		OnePerTwoPlayers,
		All
	}

	[Desc("How many TechBunkers should be spawned on the map.")]
	public class TechBunkerAmountInfo : TraitInfo, ILobbyOptions
	{
		public const string Id = "TechBunkerAmount";
		public const TechBunkerAmountType Default = TechBunkerAmountType.All;

		[Desc("The type of the bunker actor.")]
		public readonly string ActorType = "bunker_techbunker";

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyOption(
				TechBunkerAmountInfo.Id,
				"Amount",
				"How many TechBunkers should be spawned on the map.",
				true,
				0,
				new ReadOnlyDictionary<string, string>(
					new Dictionary<TechBunkerAmountType, string>
					{
						{ TechBunkerAmountType.None, "None" },
						{ TechBunkerAmountType.One, "1 per map" },
						{ TechBunkerAmountType.OnePerTwoPlayers, "1 per 2 players" },
						{ TechBunkerAmountType.All, "All" }
					}.ToDictionary(e => e.Key.ToString(), e => e.Value)),
				TechBunkerAmountInfo.Default.ToString(),
				false,
				TechBunkerInfo.LobbyOptionsCategory);
		}

		public override object Create(ActorInitializer init)
		{
			return new TechBunkerAmount(this);
		}
	}

	public class TechBunkerAmount : INotifyCreated, IPreventMapSpawn, IWorldLoaded
	{
		private readonly TechBunkerAmountInfo info;
		private readonly List<ActorReference> bunkers = new List<ActorReference>();
		private TechBunkerAmountType behavior;

		public TechBunkerAmount(TechBunkerAmountInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			behavior = (TechBunkerAmountType)Enum.Parse(
				typeof(TechBunkerAmountType),
				self.World.LobbyInfo.GlobalSettings.OptionOrDefault(TechBunkerAmountInfo.Id, TechBunkerAmountInfo.Default.ToString()));
		}

		bool IPreventMapSpawn.PreventMapSpawn(World world, ActorReference actorReference)
		{
			if (actorReference.Type != info.ActorType)
				return false;

			bunkers.Add(actorReference);

			return true;
		}

		public void WorldLoaded(World world, WorldRenderer worldRenderer)
		{
			if (behavior == TechBunkerAmountType.None)
				return;

			var numBunkers = behavior == TechBunkerAmountType.One ? 1 : world.Players.Count(p => p.Playable);

			if (behavior == TechBunkerAmountType.OnePerTwoPlayers)
				numBunkers /= 2;

			for (var i = 0; i < numBunkers && bunkers.Count > 0; i++)
			{
				var random = world.SharedRandom.Next(0, bunkers.Count);
				world.CreateActor(true, bunkers[random]);
				bunkers.RemoveAt(random);
			}
		}
	}
}

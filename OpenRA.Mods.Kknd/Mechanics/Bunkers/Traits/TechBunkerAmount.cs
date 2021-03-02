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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Bunkers.Traits
{
	public enum TechBunkerAmountType
	{
		None,
		Xtreme,
		Krossfire,
		All
	}

	[Desc("Tech bunker amount.")]
	public class TechBunkerAmountInfo : TraitInfo, ILobbyOptions
	{
		public const string Id = "TechBunkerAmount";

		[Desc("The type of the bunker actor.")]
		public readonly string ActorType = "bunker_techbunker";

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyOption(
				Id,
				"Bunker Amount",
				"TechBunker amount.",
				true,
				0,
				new ReadOnlyDictionary<string, string>(new Dictionary<TechBunkerAmountType, string>
				{
					{ TechBunkerAmountType.None, "None" },
					{ TechBunkerAmountType.Xtreme, "Xtreme (1 per map)" },
					{ TechBunkerAmountType.Krossfire, "Krossfire (1 per 2 players)" },
					{ TechBunkerAmountType.All, "All" }
				}.ToDictionary(e => e.Key.ToString(), e => e.Value)),
				TechBunkerAmountType.All.ToString(),
				false);
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
			behavior = (TechBunkerAmountType)Enum.Parse(typeof(TechBunkerAmountType),
				self.World.LobbyInfo.GlobalSettings.OptionOrDefault(TechBunkerAmountInfo.Id, TechBunkerAmountType.All.ToString()));
		}

		bool IPreventMapSpawn.PreventMapSpawn(World world, ActorReference actorReference)
		{
			if (actorReference.Type != info.ActorType)
				return false;

			bunkers.Add(actorReference);
			return true;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			if (behavior == TechBunkerAmountType.None)
				return;

			var numBunkers = behavior == TechBunkerAmountType.Xtreme ? 1 : w.Players.Count(p => p.Playable);

			if (behavior == TechBunkerAmountType.Krossfire)
				numBunkers /= 2;

			for (var i = 0; i < numBunkers && bunkers.Count > 0; i++)
			{
				var random = w.SharedRandom.Next(0, bunkers.Count);
				w.CreateActor(true, bunkers[random]);
				bunkers.RemoveAt(random);
			}
		}
	}
}

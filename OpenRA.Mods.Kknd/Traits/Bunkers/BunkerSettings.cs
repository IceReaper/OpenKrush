using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Bunkers
{
	[Desc("Selectable oilpatch oil amount in lobby.")]
	public class BunkerSettingsInfo : ITraitInfo, ILobbyOptions
	{
		public readonly string[] Values = {"Disabled", "Single Usage", "Reusable"};

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var values = new Dictionary<string, string>();

			foreach (var value in Values)
				values.Add(value, value);

			yield return new LobbyOption("bunkers", "Bunkers", "TechBunker behavior.", true, 0, new ReadOnlyDictionary<string, string>(values), "Reusable", false);
		}

		public object Create(ActorInitializer init) { return new BunkerSettings(); }
	}

	public class BunkerSettings : INotifyCreated
	{
		public bool Enabled { get; set; }
		public bool Reusable { get; set; }

		void INotifyCreated.Created(Actor self)
		{
			Enabled = self.World.LobbyInfo.GlobalSettings.OptionOrDefault("bunkers", "Reusable") != "Disabled";
			Reusable = self.World.LobbyInfo.GlobalSettings.OptionOrDefault("bunkers", "Reusable") == "Reusable";
		}
	}
}

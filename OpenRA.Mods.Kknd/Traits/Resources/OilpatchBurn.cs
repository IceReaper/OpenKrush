using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Resources
{
	[Desc("Selectable oil burn behavior in lobby.")]
	public class OilpatchBurnInfo : ITraitInfo, ILobbyOptions
	{
		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var values = new Dictionary<string, string>();

			for (var i = 0; i <= 100; i += 20)
				values.Add(i.ToString(), i + "%");

			yield return new LobbyOption("OilpatchBurn", "Oil Burn", "Percent amount of oil to burn when shot.", true, 0, new ReadOnlyDictionary<string, string>(values), "0", false);
		}

		public object Create(ActorInitializer init) { return new OilpatchBurn(); }
	}

	public class OilpatchBurn : INotifyCreated
	{
		public int Amount { get; set; }

		void INotifyCreated.Created(Actor self)
		{
			Amount = int.Parse(self.World.LobbyInfo.GlobalSettings.OptionOrDefault("OilpatchBurn", "0"));
		}
	}
}

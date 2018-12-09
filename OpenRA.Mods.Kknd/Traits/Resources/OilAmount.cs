using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Resources
{
	[Desc("Selectable oilpatch oil amount in lobby.")]
	public class OilAmountInfo : ITraitInfo, ILobbyOptions
	{
		public readonly int[] OilAmounts = {25000, 50000, 75000, 100000, -1};
		public readonly string[] OilAmountNames = {"Scarce", "Normal", "Abundant", "Maximum", "Infinite"};

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var values = new Dictionary<string, string>();

			for (var i = 0; i < OilAmountNames.Length; i++)
				values.Add(OilAmounts[i].ToString(), OilAmountNames[i]);

			var standard = OilAmounts[OilAmountNames.IndexOf("Normal")];
			yield return new LobbyOption("oilpatches", "Oilpatches", "Amount of oil every oilpatch contains.", true, 0, new ReadOnlyDictionary<string, string>(values), standard.ToString(), false);
		}

		public object Create(ActorInitializer init) { return new OilAmount(this); }
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

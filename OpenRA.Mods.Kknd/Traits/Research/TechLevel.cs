using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Research
{
	[Desc("Selectable max tech level in lobby.")]
	public class TechLevelInfo : ITraitInfo, ILobbyOptions
	{
		public readonly int TechLevels = 5;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var values = new Dictionary<string, string>();

			// If we why ever want to support 0, we need to disable research lab at all AND make sure we dont cause devision by 0 exceptions.
			for (var i = 1; i <= TechLevels; i++)
				values.Add(i.ToString(), i.ToString());

			yield return new LobbyOption("techlevel", "Techlevel", "Maximum available techlevel.", true, 0, new ReadOnlyDictionary<string, string>(values), TechLevels.ToString(), false);
		}

		public object Create(ActorInitializer init) { return new TechLevel(this); }
	}

	public class TechLevel : INotifyCreated
	{
		private readonly TechLevelInfo Info;
		public int TechLevels { get; set; }

		public TechLevel(TechLevelInfo info)
		{
			Info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			TechLevels = int.Parse(self.World.LobbyInfo.GlobalSettings.OptionOrDefault("techlevel", Info.TechLevels.ToString()));
		}
	}
}

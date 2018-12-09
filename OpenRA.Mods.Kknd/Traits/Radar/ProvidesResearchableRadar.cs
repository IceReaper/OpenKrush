using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Radar
{
	[Desc("This actor enables the radar minimap.")]
	public class ProvidesResearchableRadarInfo : ITraitInfo
	{
		[Desc("The provider level required to enable radar.")]
		public readonly int Level = 1;

		[Desc("The provider level required to show ally units.")]
		public readonly int AllyLevel = 1;

		[Desc("The provider level required to show enemy units.")]
		public readonly int EnemyLevel = 2;

		public object Create(ActorInitializer init) { return new ProvidesResearchableRadar(); }
	}

	public class ProvidesResearchableRadar { }
}

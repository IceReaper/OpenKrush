using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Kknd.Traits.Production
{
	// TODO merge to FactoryPrerequisite, make Prerequisite standard -> Make mobile base require repairbay.
	[Desc("Adds support for required tech level to Buildable.")]
	public class AdvancedBuildableInfo : BuildableInfo
	{
		[Desc("The factory level required to build this actor.")]
		public readonly int Level = 0;
	}

	public class AdvancedBuildable : Buildable { }
}

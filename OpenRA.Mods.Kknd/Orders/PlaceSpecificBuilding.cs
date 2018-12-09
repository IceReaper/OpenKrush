using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Kknd.Orders
{
	class PlaceSpecificBuildingOrderGenerator : PlaceBuildingOrderGenerator
	{
		public readonly string Name;

		public PlaceSpecificBuildingOrderGenerator(ProductionQueue queue, string name, WorldRenderer worldRenderer) : base(queue, name, worldRenderer)
		{
			Name = name;
		}
	}
}

using OpenRA.Mods.Common.Traits.Docking;

namespace OpenRA.Mods.Kknd.Traits.Repairbay
{
	public class RepairableVehicleInfo : DockableInfo
	{
		public override object Create(ActorInitializer init) { return new RepairableVehicle(this); }
	}

	public class RepairableVehicle : Dockable {
		public RepairableVehicle(RepairableVehicleInfo info) : base(info) { }
	}
}

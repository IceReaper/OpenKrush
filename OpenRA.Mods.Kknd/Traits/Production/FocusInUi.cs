using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Widgets.Ingame;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Traits.Production
{
	public class FocusInUiInfo : ConditionalTraitInfo
	{
		[FieldLoader.RequireAttribute]
		public readonly string Category = null;
		
		public override object Create(ActorInitializer init) { return new FocusInUi(this); }
	}
	
	public class FocusInUi : ConditionalTrait<FocusInUiInfo>, INotifySelected
	{
		public FocusInUi(FocusInUiInfo info) : base(info) { }

		void INotifySelected.Selected(Actor self)
		{
			if (self.Owner != self.World.LocalPlayer || IsTraitDisabled)
				return;
			
			var sidebar = Ui.Root.GetOrNull<SidebarWidget>("KKND_SIDEBAR");

			if (sidebar == null)
				return;

			sidebar.SelectFactory(self, Info.Category);
		}
	}
}

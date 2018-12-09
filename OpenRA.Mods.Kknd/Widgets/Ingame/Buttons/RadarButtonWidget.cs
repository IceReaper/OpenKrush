using System.Linq;
using OpenRA.Mods.Kknd.Traits.Radar;
using OpenRA.Mods.Kknd.Traits.Research;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets.Ingame.Buttons
{
	public class RadarButtonWidget : ButtonWidget
	{
		private bool hasRadar;

		public RadarButtonWidget(SidebarWidget sidebar) : base(sidebar, "button")
		{
			TooltipTitle = "Radar";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (!IsUsable() || e.Key != Game.ModData.Hotkeys["Radar"].GetValue().Key || e.IsRepeat || e.Event != KeyInputEvent.Down || e.Modifiers != Game.ModData.Hotkeys["Radar"].GetValue().Modifiers)
				return false;

			Active = !Active;

			sidebar.IngameUi.Radar.Visible = Active;

			return true;
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			if (base.HandleLeftClick(mi))
			{
				sidebar.IngameUi.Radar.Visible = Active;

				return true;
			}

			return false;
		}

		protected override bool IsUsable()
		{
			return hasRadar;
		}

		public override void Tick()
		{
			hasRadar = false;
			var showStances = Stance.None;

			foreach (var e in sidebar.IngameUi.World.ActorsHavingTrait<ProvidesResearchableRadar>().Where(a => a.Owner == sidebar.IngameUi.World.LocalPlayer))
			{
				var providesResearchableRadarInfo = e.Info.TraitInfo<ProvidesResearchableRadarInfo>();
				var researchable = e.Trait<Researchable>();

				if (researchable.Level < providesResearchableRadarInfo.Level)
					continue;

				hasRadar = true;

				if (researchable.Level >= providesResearchableRadarInfo.AllyLevel)
					showStances |= Stance.Ally;

				if (researchable.Level >= providesResearchableRadarInfo.EnemyLevel)
					showStances |= Stance.Enemy;
			}

			sidebar.IngameUi.Radar.ShowStances = showStances;

			if (!hasRadar)
				sidebar.IngameUi.Radar.Visible = Active = false;
		}

		protected override void DrawContents()
		{
			sidebar.Buttons.PlayFetchIndex("radar", () => 0);
			WidgetUtils.DrawSHPCentered(sidebar.Buttons.Image, center + new int2(0, Active ? 1 : 0), sidebar.IngameUi.Palette);
		}
	}
}

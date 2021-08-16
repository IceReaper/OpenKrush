#region Copyright & License Information

/*
 * Copyright 2007-2021 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Widgets.Ingame.Buttons
{
	using System.Linq;
	using Common.Widgets;
	using Mechanics.Researching.Traits;
	using OpenRA.Traits;

	public class RadarButtonWidget : ButtonWidget
	{
		private bool hasRadar;

		public RadarButtonWidget(SidebarWidget sidebar)
			: base(sidebar, "button")
		{
			TooltipTitle = "Radar";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (!IsUsable()
				|| e.IsRepeat
				|| e.Event != KeyInputEvent.Down
				|| e.Key != Game.ModData.Hotkeys["Radar"].GetValue().Key
				|| e.Modifiers != Game.ModData.Hotkeys["Radar"].GetValue().Modifiers)
				return false;

			Active = !Active;
			sidebar.IngameUi.Radar.Visible = Active;

			return true;
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			if (!base.HandleLeftClick(mi))
				return false;

			sidebar.IngameUi.Radar.Visible = Active;

			return true;
		}

		protected override bool IsUsable()
		{
			return hasRadar;
		}

		public override void Tick()
		{
			hasRadar = false;
			var showStances = PlayerRelationship.None;

			foreach (var e in sidebar.IngameUi.World.ActorsWithTrait<ProvidesResearchableRadar>()
				.Where(p => p.Actor.Owner == sidebar.IngameUi.World.LocalPlayer && !p.Trait.IsTraitDisabled))
			{
				var researchable = e.Actor.Trait<Researchable>();

				if (!researchable.IsResearched(ProvidesResearchableRadarInfo.Available))
					continue;

				hasRadar = true;

				if (researchable.IsResearched(ProvidesResearchableRadarInfo.ShowAllies))
					showStances |= PlayerRelationship.Ally;

				if (researchable.IsResearched(ProvidesResearchableRadarInfo.ShowEnemies))
					showStances |= PlayerRelationship.Enemy;
			}

			sidebar.IngameUi.Radar.ShowStances = showStances;

			if (!hasRadar)
				sidebar.IngameUi.Radar.Visible = Active = false;
		}

		protected override void DrawContents()
		{
			sidebar.Buttons.PlayFetchIndex("radar", () => 0);
			WidgetUtils.DrawSpriteCentered(sidebar.Buttons.Image, sidebar.IngameUi.Palette, center + new int2(0, Active ? 1 : 0));
		}
	}
}

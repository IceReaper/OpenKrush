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
	using Common.Widgets;
	using Mechanics.Researching.Traits;
	using System.Linq;
	using Traits;

	public class RadarButtonWidget : SidebarButtonWidget
	{
		private bool hasRadar;

		public RadarButtonWidget(SidebarWidget sidebar)
			: base(sidebar, "button")
		{
			this.TooltipTitle = "Radar";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (!this.IsUsable()
				|| e.IsRepeat
				|| e.Event != KeyInputEvent.Down
				|| e.Key != Game.ModData.Hotkeys["Radar"].GetValue().Key
				|| e.Modifiers != Game.ModData.Hotkeys["Radar"].GetValue().Modifiers)
				return false;

			this.Active = !this.Active;
			this.Sidebar.IngameUi.Radar.Visible = this.Active;

			return true;
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			if (!base.HandleLeftClick(mi))
				return false;

			this.Sidebar.IngameUi.Radar.Visible = this.Active;

			return true;
		}

		protected override bool IsUsable()
		{
			return this.hasRadar;
		}

		public override void Tick()
		{
			this.hasRadar = false;
			var showStances = PlayerRelationship.None;

			foreach (var e in this.Sidebar.IngameUi.World.ActorsWithTrait<ProvidesResearchableRadar>()
				.Where(p => p.Actor.Owner == this.Sidebar.IngameUi.World.LocalPlayer && !p.Trait.IsTraitDisabled))
			{
				var researchable = e.Actor.TraitOrDefault<Researchable>();

				if (!researchable.IsResearched(ProvidesResearchableRadarInfo.Available))
					continue;

				this.hasRadar = true;

				if (researchable.IsResearched(ProvidesResearchableRadarInfo.ShowAllies))
					showStances |= PlayerRelationship.Ally;

				if (researchable.IsResearched(ProvidesResearchableRadarInfo.ShowEnemies))
					showStances |= PlayerRelationship.Enemy;
			}

			this.Sidebar.IngameUi.Radar.ShowStances = showStances;

			if (!this.hasRadar)
				this.Sidebar.IngameUi.Radar.Visible = this.Active = false;
		}

		protected override void DrawContents()
		{
			this.Sidebar.Buttons.PlayFetchIndex("radar", () => 0);
			WidgetUtils.DrawSpriteCentered(this.Sidebar.Buttons.Image, this.Sidebar.IngameUi.Palette, this.Center + new int2(0, this.Active ? 1 : 0));
		}
	}
}

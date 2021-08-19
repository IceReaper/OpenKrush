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
	using Common.Widgets.Logic;
	using OpenRA.Widgets;
	using System.Linq;

	public class OptionsButtonWidget : SidebarButtonWidget
	{
		private Widget? menu;

		public OptionsButtonWidget(SidebarWidget sidebar)
			: base(sidebar, "button")
		{
			this.TooltipTitle = "Options";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Key != Keycode.ESCAPE || e.IsRepeat || e.Event != KeyInputEvent.Down || this.menu != null || e.Modifiers != Modifiers.None)
				return false;

			this.Active = true;
			this.ShowMenu();

			return true;
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			if (!base.HandleLeftClick(mi))
				return false;

			if (this.Active)
				this.ShowMenu();

			return true;
		}

		private void ShowMenu()
		{
			if (this.Sidebar.IngameUi.World.LobbyInfo.NonBotClients.Count() == 1)
				this.Sidebar.IngameUi.World.SetPauseState(true);

			var widgetArgs = new WidgetArgs
			{
				{ "initialPanel", IngameInfoPanel.AutoSelect },
				{
					"onExit", () =>
					{
						if (this.Sidebar.IngameUi.World.LobbyInfo.NonBotClients.Count() == 1)
							this.Sidebar.IngameUi.World.SetPauseState(false);

						Ui.Root.Get("MENU_ROOT").RemoveChild(this.menu);
						this.Active = false;
						this.menu = null;
					}
				}
			};

			this.menu = Game.LoadWidget(this.Sidebar.IngameUi.World, "INGAME_MENU", Ui.Root.Get("MENU_ROOT"), widgetArgs);
		}

		protected override void DrawContents()
		{
			this.Sidebar.Buttons.PlayFetchIndex("options", () => 0);
			WidgetUtils.DrawSpriteCentered(this.Sidebar.Buttons.Image, this.Sidebar.IngameUi.Palette, this.Center + new int2(0, this.Active ? 1 : 0));
		}
	}
}

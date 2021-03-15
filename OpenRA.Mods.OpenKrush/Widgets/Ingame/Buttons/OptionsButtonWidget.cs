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
	using Common.Widgets.Logic;
	using OpenRA.Widgets;

	public class OptionsButtonWidget : ButtonWidget
	{
		private Widget menu;

		public OptionsButtonWidget(SidebarWidget sidebar)
			: base(sidebar, "button")
		{
			TooltipTitle = "Options";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Key == Keycode.ESCAPE && !e.IsRepeat && e.Event == KeyInputEvent.Down && menu == null && e.Modifiers == Modifiers.None)
			{
				Active = true;
				ShowMenu();

				return true;
			}

			return false;
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			if (base.HandleLeftClick(mi))
			{
				if (Active)
					ShowMenu();

				return true;
			}

			return false;
		}

		void ShowMenu()
		{
			if (sidebar.IngameUi.World.LobbyInfo.NonBotClients.Count() == 1)
				sidebar.IngameUi.World.SetPauseState(true);

			var widgetArgs = new WidgetArgs();
			widgetArgs.Add("activePanel", IngameInfoPanel.AutoSelect);

			widgetArgs.Add(
				"onExit",
				() =>
				{
					if (sidebar.IngameUi.World.LobbyInfo.NonBotClients.Count() == 1)
						sidebar.IngameUi.World.SetPauseState(false);

					Ui.Root.Get("MENU_ROOT").RemoveChild(menu);
					Active = false;
					menu = null;
				});

			menu = Game.LoadWidget(sidebar.IngameUi.World, "INGAME_MENU", Ui.Root.Get("MENU_ROOT"), widgetArgs);
		}

		protected override void DrawContents()
		{
			sidebar.Buttons.PlayFetchIndex("options", () => 0);
			WidgetUtils.DrawSHPCentered(sidebar.Buttons.Image, center + new int2(0, Active ? 1 : 0), sidebar.IngameUi.Palette);
		}
	}
}

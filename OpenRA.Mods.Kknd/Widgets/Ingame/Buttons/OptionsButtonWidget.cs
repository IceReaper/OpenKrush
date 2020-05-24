#region Copyright & License Information
/*
 * Copyright 2016-2018 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets.Ingame.Buttons
{
	public class OptionsButtonWidget : ButtonWidget
	{
		private Widget menu;

		public OptionsButtonWidget(SidebarWidget sidebar) : base(sidebar, "button")
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
			widgetArgs.Add("onExit", () =>
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

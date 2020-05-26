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
using OpenRA.Mods.Kknd.Orders;
using OpenRA.Mods.Kknd.Traits.Production;

namespace OpenRA.Mods.Kknd.Widgets.Ingame.Buttons
{
	public class SellButtonWidget : ButtonWidget
	{
		private bool sellableActors;

		public SellButtonWidget(SidebarWidget sidebar)
			: base(sidebar, "button")
		{
			TooltipTitle = "Sell";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (IsUsable() && !e.IsRepeat && e.Event == KeyInputEvent.Down
				&& e.Key == Game.ModData.Hotkeys["Sell"].GetValue().Key && e.Modifiers == Game.ModData.Hotkeys["Sell"].GetValue().Modifiers)
			{
				Active = !Active;

				if (Active)
					sidebar.IngameUi.World.OrderGenerator = new SellOrderGenerator();
				else if (sidebar.IngameUi.World.OrderGenerator != null && sidebar.IngameUi.World.OrderGenerator is SellOrderGenerator)
					sidebar.IngameUi.World.CancelInputMode();

				return true;
			}

			return false;
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			if (base.HandleLeftClick(mi))
			{
				if (Active)
					sidebar.IngameUi.World.OrderGenerator = new SellOrderGenerator();
				else if (sidebar.IngameUi.World.OrderGenerator != null && sidebar.IngameUi.World.OrderGenerator is SellOrderGenerator)
					sidebar.IngameUi.World.CancelInputMode();

				return true;
			}

			return false;
		}

		protected override bool IsUsable()
		{
			return sellableActors;
		}

		public override void Tick()
		{
			sellableActors = sidebar.IngameUi.World.ActorsWithTrait<SelfConstructing>().Any(e => e.Actor.Owner == sidebar.IngameUi.World.LocalPlayer);
			Active = sidebar.IngameUi.World.OrderGenerator != null && sidebar.IngameUi.World.OrderGenerator is SellOrderGenerator;
		}

		protected override void DrawContents()
		{
			sidebar.Buttons.PlayFetchIndex("sell", () => 0);
			WidgetUtils.DrawSHPCentered(sidebar.Buttons.Image, center + new int2(0, Active ? 1 : 0), sidebar.IngameUi.Palette);
		}
	}
}

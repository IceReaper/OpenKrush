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
using OpenRA.Mods.Kknd.Orders;
using OpenRA.Mods.Kknd.Traits.Technicians;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets.Ingame.Buttons
{
	public class RepairButtonWidget : ButtonWidget
	{
		private bool technicians;

		public RepairButtonWidget(SidebarWidget sidebar) : base(sidebar, "button")
		{
			TooltipTitle = "Repair";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (IsUsable() && e.Key == Game.ModData.Hotkeys["Repair"].GetValue().Key && !e.IsRepeat && e.Event == KeyInputEvent.Down && e.Modifiers == Game.ModData.Hotkeys["Repair"].GetValue().Modifiers)
			{
				Active = !Active;

				if (Active)
					sidebar.IngameUi.World.OrderGenerator = new TechnicianEnterOrderGenerator();
				else if (sidebar.IngameUi.World.OrderGenerator != null && sidebar.IngameUi.World.OrderGenerator is TechnicianEnterOrderGenerator)
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
					sidebar.IngameUi.World.OrderGenerator = new TechnicianEnterOrderGenerator();
				else if (sidebar.IngameUi.World.OrderGenerator != null && sidebar.IngameUi.World.OrderGenerator is TechnicianEnterOrderGenerator)
					sidebar.IngameUi.World.CancelInputMode();

				return true;
			}

			return false;
		}

		protected override bool IsUsable()
		{
			return technicians;
		}

		public override void Tick()
		{
			technicians = sidebar.IngameUi.World.ActorsWithTrait<Technician>().Any(e => e.Actor.Owner == sidebar.IngameUi.World.LocalPlayer && e.Actor.IsIdle);
			Active = sidebar.IngameUi.World.OrderGenerator != null && sidebar.IngameUi.World.OrderGenerator is TechnicianEnterOrderGenerator;
		}

		protected override void DrawContents()
		{
			sidebar.Buttons.PlayFetchIndex("repair", () => 0);
			WidgetUtils.DrawSHPCentered(sidebar.Buttons.Image, center + new int2(0, Active ? 1 : 0), sidebar.IngameUi.Palette);
		}
	}
}

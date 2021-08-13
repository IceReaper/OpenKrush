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
	using Mechanics.Technicians.Orders;
	using Mechanics.Technicians.Traits;

	public class RepairButtonWidget : ButtonWidget
	{
		private bool technicians;

		public RepairButtonWidget(SidebarWidget sidebar)
			: base(sidebar, "button")
		{
			TooltipTitle = "Repair";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (IsUsable()
				&& !e.IsRepeat
				&& e.Event == KeyInputEvent.Down
				&& e.Key == Game.ModData.Hotkeys["Repair"].GetValue().Key
				&& e.Modifiers == Game.ModData.Hotkeys["Repair"].GetValue().Modifiers)
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
			WidgetUtils.DrawSpriteCentered(sidebar.Buttons.Image, sidebar.IngameUi.Palette, center + new int2(0, Active ? 1 : 0));
		}
	}
}

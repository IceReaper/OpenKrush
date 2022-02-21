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

namespace OpenRA.Mods.OpenKrush.Widgets.Ingame.Buttons;

using Common.Widgets;
using Mechanics.Technicians.Orders;
using Mechanics.Technicians.Traits;

public class RepairButtonWidget : SidebarButtonWidget
{
	private bool technicians;

	public RepairButtonWidget(SidebarWidget sidebar)
		: base(sidebar, "button")
	{
		this.TooltipTitle = "Repair";
	}

	public override bool HandleKeyPress(KeyInput e)
	{
		if (!this.IsUsable()
			|| e.IsRepeat
			|| e.Event != KeyInputEvent.Down
			|| e.Key != Game.ModData.Hotkeys["Repair"].GetValue().Key
			|| e.Modifiers != Game.ModData.Hotkeys["Repair"].GetValue().Modifiers)
			return false;

		this.Active = !this.Active;

		if (this.Active)
			this.Sidebar.IngameUi.World.OrderGenerator = new TechnicianEnterOrderGenerator();
		else if (this.Sidebar.IngameUi.World.OrderGenerator is TechnicianEnterOrderGenerator)
			this.Sidebar.IngameUi.World.CancelInputMode();

		return true;
	}

	protected override bool HandleLeftClick(MouseInput mi)
	{
		if (!base.HandleLeftClick(mi))
			return false;

		if (this.Active)
			this.Sidebar.IngameUi.World.OrderGenerator = new TechnicianEnterOrderGenerator();
		else if (this.Sidebar.IngameUi.World.OrderGenerator is TechnicianEnterOrderGenerator)
			this.Sidebar.IngameUi.World.CancelInputMode();

		return true;
	}

	protected override bool IsUsable()
	{
		return this.technicians;
	}

	public override void Tick()
	{
		this.technicians = this.Sidebar.IngameUi.World.ActorsWithTrait<Technician>()
			.Any(e => e.Actor.Owner == this.Sidebar.IngameUi.World.LocalPlayer && e.Actor.IsIdle);

		this.Active = this.Sidebar.IngameUi.World.OrderGenerator is TechnicianEnterOrderGenerator;
	}

	protected override void DrawContents()
	{
		this.Sidebar.Buttons.PlayFetchIndex("repair", () => 0);
		WidgetUtils.DrawSpriteCentered(this.Sidebar.Buttons.Image, this.Sidebar.IngameUi.Palette, this.Center + new int2(0, this.Active ? 1 : 0));
	}
}

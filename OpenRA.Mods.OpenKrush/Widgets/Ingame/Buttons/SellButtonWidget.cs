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
using Mechanics.Construction.Orders;
using Mechanics.Construction.Traits;

public class SellButtonWidget : SidebarButtonWidget
{
	private bool sellableActors;

	public SellButtonWidget(SidebarWidget sidebar)
		: base(sidebar, "button")
	{
		this.TooltipTitle = "Sell";
	}

	public override bool HandleKeyPress(KeyInput e)
	{
		if (!this.IsUsable()
			|| e.IsRepeat
			|| e.Event != KeyInputEvent.Down
			|| e.Key != Game.ModData.Hotkeys["Sell"].GetValue().Key
			|| e.Modifiers != Game.ModData.Hotkeys["Sell"].GetValue().Modifiers)
			return false;

		this.Active = !this.Active;

		if (this.Active)
			this.Sidebar.IngameUi.World.OrderGenerator = new SellOrderGenerator();
		else if (this.Sidebar.IngameUi.World.OrderGenerator is SellOrderGenerator)
			this.Sidebar.IngameUi.World.CancelInputMode();

		return true;
	}

	protected override bool HandleLeftClick(MouseInput mi)
	{
		if (!base.HandleLeftClick(mi))
			return false;

		if (this.Active)
			this.Sidebar.IngameUi.World.OrderGenerator = new SellOrderGenerator();
		else if (this.Sidebar.IngameUi.World.OrderGenerator is SellOrderGenerator)
			this.Sidebar.IngameUi.World.CancelInputMode();

		return true;
	}

	protected override bool IsUsable()
	{
		return this.sellableActors;
	}

	public override void Tick()
	{
		this.sellableActors = this.Sidebar.IngameUi.World.ActorsWithTrait<SelfConstructing>()
			.Any(e => e.Actor.Owner == this.Sidebar.IngameUi.World.LocalPlayer);

		this.Active = this.Sidebar.IngameUi.World.OrderGenerator is SellOrderGenerator;
	}

	protected override void DrawContents()
	{
		this.Sidebar.Buttons.PlayFetchIndex("sell", () => 0);
		WidgetUtils.DrawSpriteCentered(this.Sidebar.Buttons.Image, this.Sidebar.IngameUi.Palette, this.Center + new int2(0, this.Active ? 1 : 0));
	}
}

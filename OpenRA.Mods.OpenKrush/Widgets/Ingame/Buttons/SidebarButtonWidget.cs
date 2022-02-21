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
using OpenRA.Widgets;
using Primitives;

public abstract class SidebarButtonWidget : Widget
{
	public const int Size = 48;

	protected readonly SidebarWidget Sidebar;
	protected string Type;
	protected int2 Center;
	public bool Active;

	public string? TooltipTitle = null;
	public string? TooltipText = null;

	protected SidebarButtonWidget(SidebarWidget sidebar, string type)
	{
		this.Sidebar = sidebar;
		this.Type = type;

		this.Bounds = new(0, 0, SidebarButtonWidget.Size, SidebarButtonWidget.Size);
	}

	public override void MouseEntered()
	{
		if (!this.IsUsable())
			return;

		this.Sidebar.IngameUi.Tooltip.TooltipTitle = this.TooltipTitle;
		this.Sidebar.IngameUi.Tooltip.TooltipText = this.TooltipText;

		this.Sidebar.IngameUi.Tooltip.Bounds.X = this.RenderBounds.X;
		this.Sidebar.IngameUi.Tooltip.Bounds.Y = this.RenderBounds.Y + SidebarButtonWidget.Size / 2;

		this.Sidebar.IngameUi.Tooltip.Visible = true;
	}

	public override void MouseExited()
	{
		this.Sidebar.IngameUi.Tooltip.Visible = false;
	}

	public override bool HandleMouseInput(MouseInput mi)
	{
		if (!this.RenderBounds.Contains(mi.Location))
			Ui.MouseOverWidget = null;
		else if (mi.Event == MouseInputEvent.Down && this.IsUsable())
			return (mi.Button == MouseButton.Left && this.HandleLeftClick(mi)) || (mi.Button == MouseButton.Right && this.HandleRightClick(mi));

		return false;
	}

	protected virtual bool HandleLeftClick(MouseInput mi)
	{
		Game.Sound.PlayNotification(this.Sidebar.IngameUi.World.Map.Rules, null, "Sounds", "ClickSound", null);
		this.Active = !this.Active;

		return true;
	}

	protected virtual bool HandleRightClick(MouseInput mi)
	{
		return false;
	}

	protected virtual bool IsUsable()
	{
		return true;
	}

	public override void Draw()
	{
		this.Center = new(this.RenderBounds.X + SidebarButtonWidget.Size / 2, this.RenderBounds.Y + SidebarButtonWidget.Size / 2);

		WidgetUtils.FillRectWithColor(
			new(this.RenderBounds.X, this.RenderBounds.Y, SidebarButtonWidget.Size, SidebarButtonWidget.Size),
			Color.FromArgb(255, 0, 0, 0)
		);

		this.Sidebar.Buttons.PlayFetchIndex(this.Active && this.IsUsable() ? $"{this.Type}-down" : this.Type, () => 0);
		WidgetUtils.DrawSpriteCentered(this.Sidebar.Buttons.Image, this.Sidebar.IngameUi.Palette, this.Center);

		if (this.IsUsable())
			this.DrawContents();

		if (this.Active && this.IsUsable())
			WidgetUtils.FillRectWithColor(this.RenderBounds, Color.FromArgb(128, 0, 0, 0));
	}

	protected abstract void DrawContents();
}

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
	using OpenRA.Widgets;
	using Primitives;

	public abstract class ButtonWidget : Widget
	{
		public const int Size = 48;

		protected readonly SidebarWidget sidebar;
		protected string type;
		protected int2 center;
		public bool Active;

		public string TooltipTitle = null;
		public string TooltipText = null;

		protected ButtonWidget(SidebarWidget sidebar, string type)
		{
			this.sidebar = sidebar;
			this.type = type;

			Bounds = new Rectangle(0, 0, ButtonWidget.Size, ButtonWidget.Size);
		}

		public override void MouseEntered()
		{
			if (!IsUsable())
				return;

			sidebar.IngameUi.Tooltip.TooltipTitle = TooltipTitle;
			sidebar.IngameUi.Tooltip.TooltipText = TooltipText;

			sidebar.IngameUi.Tooltip.Bounds.X = RenderBounds.X;
			sidebar.IngameUi.Tooltip.Bounds.Y = RenderBounds.Y + ButtonWidget.Size / 2;

			sidebar.IngameUi.Tooltip.Visible = true;
		}

		public override void MouseExited()
		{
			sidebar.IngameUi.Tooltip.Visible = false;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (!RenderBounds.Contains(mi.Location))
				Ui.MouseOverWidget = null;
			else if (mi.Event == MouseInputEvent.Down && IsUsable())
				return (mi.Button == MouseButton.Left && HandleLeftClick(mi)) || (mi.Button == MouseButton.Right && HandleRightClick(mi));

			return false;
		}

		protected virtual bool HandleLeftClick(MouseInput mi)
		{
			Game.Sound.PlayNotification(sidebar.IngameUi.World.Map.Rules, null, "Sounds", "ClickSound", null);
			Active = !Active;

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
			center = new int2(RenderBounds.X + ButtonWidget.Size / 2, RenderBounds.Y + ButtonWidget.Size / 2);

			WidgetUtils.FillRectWithColor(new Rectangle(RenderBounds.X, RenderBounds.Y, ButtonWidget.Size, ButtonWidget.Size), Color.FromArgb(255, 0, 0, 0));

			sidebar.Buttons.PlayFetchIndex(Active && IsUsable() ? $"{type}-down" : type, () => 0);
			WidgetUtils.DrawSpriteCentered(sidebar.Buttons.Image, sidebar.IngameUi.Palette, center);

			if (IsUsable())
				DrawContents();

			if (Active && IsUsable())
				WidgetUtils.FillRectWithColor(RenderBounds, Color.FromArgb(128, 0, 0, 0));
		}

		protected abstract void DrawContents();
	}
}

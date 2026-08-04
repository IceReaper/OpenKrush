#region Copyright & License Information

/*
 * Copyright 2007-2022 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Widgets.Ingame;

using Common.Widgets;
using Graphics;
using OpenRA.Widgets;
using Primitives;

public class TooltipWidget : Widget
{
	public static TooltipWidget? Instance { get; private set; }

	private readonly SpriteFont tooltipTitleFont;
	private readonly SpriteFont tooltipTextFont;
	public string? TooltipTitle = null;
	public string? TooltipText = null;

	private int showTicks;
	private bool wasVisible;
	private const int AutoHideDelay = 60;

	public TooltipWidget()
	{
		Instance = this;
		this.tooltipTitleFont = Game.Renderer.Fonts["Regular"];
		this.tooltipTextFont = Game.Renderer.Fonts["Tiny"];
		this.Visible = false;
	}

	public void ShowCentered(string title, string? text = null)
	{
		this.TooltipTitle = title;
		this.TooltipText = text;
		this.showTicks = 0;
		this.Visible = true;
	}

	public override void Tick()
	{
		if (!this.Visible)
		{
			this.wasVisible = false;
			return;
		}

		if (!this.wasVisible)
		{
			this.wasVisible = true;
			this.showTicks = 0;
		}

		this.showTicks++;

		if (this.showTicks >= AutoHideDelay)
		{
			this.Visible = false;
			this.wasVisible = false;
			this.showTicks = 0;
		}
	}

	public override void Draw()
	{
		var tooltipTitleMeasure = this.TooltipTitle == null ? int2.Zero : this.tooltipTitleFont.Measure(this.TooltipTitle);
		var tooltipTextMeasure = this.TooltipText == null ? int2.Zero : this.tooltipTextFont.Measure(this.TooltipText);

		var w = System.Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) + 12;
		var h = tooltipTitleMeasure.Y + tooltipTextMeasure.Y + 12;

		var x = Game.Renderer.Resolution.Width / 2 - w / 2;
		var y = Game.Renderer.Resolution.Height / 3;

		WidgetUtils.FillRectWithColor(new(x, y, w, h), Color.FromArgb(255, 255, 255, 255));
		WidgetUtils.FillRectWithColor(new(x + 1, y + 1, w - 2, h - 2), Color.FromArgb(255, 0, 0, 0));

		if (this.TooltipTitle != null)
			this.tooltipTitleFont.DrawText(this.TooltipTitle, new int2(x + 6, y + 5), Color.White);

		if (this.TooltipText != null)
			this.tooltipTextFont.DrawText(this.TooltipText, new int2(x + 6, y + 6 + tooltipTitleMeasure.Y), Color.White);
	}
}

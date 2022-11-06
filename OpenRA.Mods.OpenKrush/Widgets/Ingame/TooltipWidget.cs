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
	private readonly SpriteFont tooltipTitleFont;
	private readonly SpriteFont tooltipTextFont;
	public string? TooltipTitle = null;
	public string? TooltipText = null;

	public TooltipWidget()
	{
		this.tooltipTitleFont = Game.Renderer.Fonts["Regular"];
		this.tooltipTextFont = Game.Renderer.Fonts["Tiny"];

		this.Visible = false;
	}

	public override void Draw()
	{
		var tooltipTitleMeasure = this.TooltipTitle == null ? int2.Zero : this.tooltipTitleFont.Measure(this.TooltipTitle);
		var tooltipTextMeasure = this.TooltipText == null ? int2.Zero : this.tooltipTextFont.Measure(this.TooltipText);

		WidgetUtils.FillRectWithColor(
			new(
				this.RenderBounds.X - Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) - 12,
				this.RenderBounds.Y - (tooltipTitleMeasure.Y + tooltipTextMeasure.Y) / 2 - 6,
				Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) + 12,
				tooltipTitleMeasure.Y + tooltipTextMeasure.Y + 12
			),
			Color.FromArgb(255, 255, 255, 255)
		);

		WidgetUtils.FillRectWithColor(
			new(
				this.RenderBounds.X - Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) - 11,
				this.RenderBounds.Y - (tooltipTitleMeasure.Y + tooltipTextMeasure.Y) / 2 - 5,
				Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) + 10,
				tooltipTitleMeasure.Y + tooltipTextMeasure.Y + 10
			),
			Color.FromArgb(255, 0, 0, 0)
		);

		if (this.TooltipTitle != null)
		{
			this.tooltipTitleFont.DrawText(
				this.TooltipTitle,
				new int2(
					this.RenderBounds.X - Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) - 6,
					this.RenderBounds.Y - (tooltipTitleMeasure.Y + tooltipTextMeasure.Y) / 2 - 5
				),
				Color.White
			);
		}

		if (this.TooltipText != null)
		{
			this.tooltipTextFont.DrawText(
				this.TooltipText,
				new int2(
					this.RenderBounds.X - Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) - 6,
					this.RenderBounds.Y - (tooltipTitleMeasure.Y + tooltipTextMeasure.Y) / 2 + tooltipTitleMeasure.Y
				),
				Color.White
			);
		}
	}
}

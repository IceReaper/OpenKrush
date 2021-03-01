#region Copyright & License Information
/*
 * Copyright 2007-2021 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets.Ingame
{
	public class TooltipWidget : Widget
	{
		private SpriteFont tooltipTitleFont;
		private SpriteFont tooltipTextFont;
		public string TooltipTitle = null;
		public string TooltipText = null;

		public TooltipWidget()
		{
			tooltipTitleFont = Game.Renderer.Fonts["Regular"];
			tooltipTextFont = Game.Renderer.Fonts["Tiny"];

			Visible = false;
		}

		public override void Draw()
		{
			var tooltipTitleMeasure = TooltipTitle == null ? int2.Zero : tooltipTitleFont.Measure(TooltipTitle);
			var tooltipTextMeasure = TooltipText == null ? int2.Zero : tooltipTextFont.Measure(TooltipText);

			WidgetUtils.FillRectWithColor(new Rectangle(
				RenderBounds.X - Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) - 12,
				RenderBounds.Y - (tooltipTitleMeasure.Y + tooltipTextMeasure.Y) / 2 - 6,
				Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) + 12,
				tooltipTitleMeasure.Y + tooltipTextMeasure.Y + 12), Color.FromArgb(255, 255, 255, 255));

			WidgetUtils.FillRectWithColor(new Rectangle(
				RenderBounds.X - Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) - 11,
				RenderBounds.Y - (tooltipTitleMeasure.Y + tooltipTextMeasure.Y) / 2 - 5,
				Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) + 10,
				tooltipTitleMeasure.Y + tooltipTextMeasure.Y + 10), Color.FromArgb(255, 0, 0, 0));

			if (TooltipTitle != null)
			{
				tooltipTitleFont.DrawText(TooltipTitle, new int2(
					RenderBounds.X - Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) - 6,
					RenderBounds.Y - (tooltipTitleMeasure.Y + tooltipTextMeasure.Y) / 2 - 5), Color.White);
			}

			if (TooltipText != null)
			{
				tooltipTextFont.DrawText(TooltipText, new int2(
					RenderBounds.X - Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) - 6,
					RenderBounds.Y - (tooltipTitleMeasure.Y + tooltipTextMeasure.Y) / 2 + tooltipTitleMeasure.Y), Color.White);
			}
		}
	}
}

using System;
using System.Drawing;
using OpenRA.Graphics;
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
				tooltipTitleMeasure.Y + tooltipTextMeasure.Y + 12
			), Color.FromArgb(255, 255, 255, 255));

			WidgetUtils.FillRectWithColor(new Rectangle(
				RenderBounds.X - Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) - 11,
				RenderBounds.Y - (tooltipTitleMeasure.Y + tooltipTextMeasure.Y) / 2 - 5,
				Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) + 10,
				tooltipTitleMeasure.Y + tooltipTextMeasure.Y + 10
			), Color.FromArgb(255, 0, 0, 0));

			if (TooltipTitle != null)
			{
				tooltipTitleFont.DrawText(TooltipTitle, new int2(
					RenderBounds.X - Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) - 6,
					RenderBounds.Y - (tooltipTitleMeasure.Y + tooltipTextMeasure.Y) / 2
				), Color.White);
			}

			if (TooltipText != null)
			{
				tooltipTextFont.DrawText(TooltipText, new int2(
					RenderBounds.X - Math.Max(tooltipTitleMeasure.X, tooltipTextMeasure.X) - 6,
					RenderBounds.Y - (tooltipTitleMeasure.Y + tooltipTextMeasure.Y) / 2 + tooltipTitleMeasure.Y
				), Color.White);
			}
		}
	}
}

using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets.Ingame
{
	public class StatusWidget : Widget {
		private IngameUiWidget ingameUi;
		private readonly PlayerResources playerResources;
		private readonly SpriteFont font;
		private int powerHeight;

		public StatusWidget(IngameUiWidget ingameUi)
		{
			this.ingameUi = ingameUi;
			playerResources = ingameUi.World.LocalPlayer.PlayerActor.Trait<PlayerResources>();
			Game.Renderer.Fonts.TryGetValue("Bold", out font);
			powerHeight = font.Measure("00:00").Y;
			Resize();
		}

		public override void Resize()
		{
			Bounds = new Rectangle((Game.Renderer.Resolution.Width - 180) / 2, 0, 180, 28);
		}

		public override void Tick()
		{
			var numPowers = ingameUi.World.Players.Sum(player => player.PlayerActor.Trait<SupportPowerManager>().Powers.Count(p => p.Value.Active));
			Bounds.Height = 28 + (numPowers / 4 + (numPowers % 4 == 0 ? 0 : 1)) * powerHeight + (numPowers > 0 ? 5 : 0);
		}

		public override void Draw()
		{
			WidgetUtils.FillRectWithColor(RenderBounds, Color.White);
			WidgetUtils.FillRectWithColor(new Rectangle(RenderBounds.X + 1, RenderBounds.Y, RenderBounds.Width - 2, RenderBounds.Height - 1), Color.Black);

			DrawTimer();
			DrawResources();
			DrawSuperweapons();
		}

		private void DrawTimer()
		{
			var text = WidgetUtils.FormatTime(ingameUi.World.WorldTick, false, ingameUi.World.Timestep);
			font.DrawText(text, new int2(RenderBounds.X + 10, RenderBounds.Y + 5), Color.White);
		}

		private void DrawResources()
		{
			var text = (playerResources.Cash + playerResources.Resources).ToString();
			font.DrawText(text, new int2(RenderBounds.X + RenderBounds.Width - 10 - font.Measure(text).X, RenderBounds.Y + 5), Color.White);
		}

		private void DrawSuperweapons()
		{
			var index = 0;

			foreach (var player in ingameUi.World.Players)
			{
				var powers = player.PlayerActor.Trait<SupportPowerManager>().Powers.Where(p => p.Value.Active).OrderBy(p => p.Value.RemainingTime);

				foreach (var power in powers)
				{
					var text = WidgetUtils.FormatTime(power.Value.RemainingTime, false, ingameUi.World.Timestep);
					font.DrawTextWithContrast(text, new int2(
						RenderBounds.X + 10 + index % 4 * ((RenderBounds.Width - 20) / 4),
						RenderBounds.Y + 25 + index / 4 * powerHeight
					), player.Color.RGB, player.Color.L > 50 ? Color.Black : Color.White, 1);
					index++;
				}
			}
		}
	}
}

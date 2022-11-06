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

using Common.Traits;
using Common.Widgets;
using Graphics;
using OpenRA.Widgets;
using Primitives;

public class StatusWidget : Widget
{
	private readonly IngameUiWidget ingameUi;
	private readonly PlayerResources playerResources;
	private readonly SpriteFont? font;
	private readonly int powerHeight;

	public StatusWidget(IngameUiWidget ingameUi)
	{
		this.ingameUi = ingameUi;
		this.playerResources = ingameUi.World.LocalPlayer.PlayerActor.TraitOrDefault<PlayerResources>();
		Game.Renderer.Fonts.TryGetValue("Bold", out this.font);
		this.powerHeight = this.font?.Measure("00:00").Y ?? 0;
		this.Resize();
	}

	private void Resize()
	{
		this.Bounds = new((Game.Renderer.Resolution.Width - 180) / 2, 0, 180, 28);
	}

	public override void Tick()
	{
		var numPowers = this.ingameUi.World.Players.Sum(player => player.PlayerActor.TraitOrDefault<SupportPowerManager>().Powers.Count(p => p.Value.Active));

		this.Bounds.Height = 28 + (numPowers / 4 + (numPowers % 4 == 0 ? 0 : 1)) * this.powerHeight + (numPowers > 0 ? 5 : 0);
	}

	public override void Draw()
	{
		WidgetUtils.FillRectWithColor(this.RenderBounds, Color.White);

		WidgetUtils.FillRectWithColor(
			new(this.RenderBounds.X + 1, this.RenderBounds.Y, this.RenderBounds.Width - 2, this.RenderBounds.Height - 1),
			Color.Black
		);

		this.DrawTimer();
		this.DrawResources();
		this.DrawSuperweapons();
	}

	private void DrawTimer()
	{
		var text = WidgetUtils.FormatTime(this.ingameUi.World.WorldTick, false, this.ingameUi.World.Timestep);
		this.font?.DrawText(text, new int2(this.RenderBounds.X + 10, this.RenderBounds.Y + 5), Color.White);
	}

	private void DrawResources()
	{
		var text = (this.playerResources.Cash + this.playerResources.Resources).ToString();

		this.font?.DrawText(
			text,
			new int2(this.RenderBounds.X + this.RenderBounds.Width - 10 - this.font.Measure(text).X, this.RenderBounds.Y + 5),
			Color.White
		);
	}

	private void DrawSuperweapons()
	{
		var index = 0;

		foreach (var player in this.ingameUi.World.Players)
		{
			var powers = player.PlayerActor.TraitOrDefault<SupportPowerManager>().Powers.Where(p => p.Value.Active).OrderBy(p => p.Value.RemainingTicks);

			foreach (var power in powers)
			{
				var text = WidgetUtils.FormatTime(power.Value.RemainingTicks, false, this.ingameUi.World.Timestep);

				this.font?.DrawTextWithContrast(
					text,
					new int2(
						this.RenderBounds.X + 10 + index % 4 * ((this.RenderBounds.Width - 20) / 4),
						this.RenderBounds.Y + 25 + index / 4 * this.powerHeight
					),
					player.Color,
					player.Color.GetBrightness() > .5 ? Color.Black : Color.White,
					1
				);

				index++;
			}
		}
	}
}

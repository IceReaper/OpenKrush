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

namespace OpenRA.Mods.OpenKrush.Widgets.Ingame
{
	using Common.Traits;
	using Common.Widgets;
	using Graphics;
	using OpenRA.Widgets;
	using Primitives;
	using Traits;

	public class RadarWidget : Widget
	{
		private const int Scale = 2;

		private readonly IngameUiWidget ingameUi;

		private readonly Sheet radarSheet;
		private readonly byte[] radarData;
		private readonly Sprite terrainSprite;
		private readonly Sprite shroudSprite;

		private bool useStanceColor;

		public PlayerRelationship ShowStances { get; set; }

		public RadarWidget(IngameUiWidget ingameUi)
		{
			this.ingameUi = ingameUi;

			this.radarSheet = new(SheetType.BGRA, new Size(ingameUi.World.Map.MapSize.X, ingameUi.World.Map.MapSize.Y * 2).NextPowerOf2());
			this.radarSheet.CreateBuffer();
			this.radarData = this.radarSheet.GetData();

			this.terrainSprite = new(this.radarSheet, new(0, 0, ingameUi.World.Map.MapSize.X, ingameUi.World.Map.MapSize.Y), TextureChannel.RGBA);

			this.shroudSprite = new(
				this.radarSheet,
				new(0, ingameUi.World.Map.MapSize.Y, ingameUi.World.Map.MapSize.X, ingameUi.World.Map.MapSize.Y),
				TextureChannel.RGBA
			);

			this.DrawTerrain();
			this.Resize();
			this.Visible = false;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Key != Game.ModData.Hotkeys["TogglePlayerStanceColor"].GetValue().Key || e.IsRepeat || e.Event != KeyInputEvent.Down)
				return false;

			this.useStanceColor = !this.useStanceColor;

			return true;
		}

		private void Resize()
		{
			this.Bounds = new(
				0,
				Game.Renderer.Resolution.Height - this.ingameUi.World.Map.MapSize.Y * RadarWidget.Scale,
				this.ingameUi.World.Map.MapSize.X * RadarWidget.Scale,
				this.ingameUi.World.Map.MapSize.Y * RadarWidget.Scale
			);
		}

		private void DrawTerrain()
		{
			// TODO instead of using this colors, try a correct thumbnail variant.
			for (var y = 0; y < this.ingameUi.World.Map.MapSize.Y; y++)
			for (var x = 0; x < this.ingameUi.World.Map.MapSize.X; x++)
			{
				var type = this.ingameUi.World.Map.Rules.TerrainInfo.GetTerrainInfo(this.ingameUi.World.Map.Tiles[new MPos(x, y)]);
				this.radarData[(y * this.radarSheet.Size.Width + x) * 4] = type.MinColor.B;
				this.radarData[(y * this.radarSheet.Size.Width + x) * 4 + 1] = type.MinColor.G;
				this.radarData[(y * this.radarSheet.Size.Width + x) * 4 + 2] = type.MinColor.R;
				this.radarData[(y * this.radarSheet.Size.Width + x) * 4 + 3] = 0xff;
			}
		}

		private void UpdateShroud()
		{
			var rp = this.ingameUi.World.RenderPlayer;

			for (var y = 0; y < this.ingameUi.World.Map.MapSize.Y; y++)
			for (var x = 0; x < this.ingameUi.World.Map.MapSize.X; x++)
			{
				var color = Color.FromArgb(0, Color.Black);

				if (rp != null)
				{
					var pos = new MPos(x, y);

					if (!rp.Shroud.IsExplored(pos))
						color = Color.FromArgb(255, Color.Black);
					else if (!rp.Shroud.IsVisible(pos))
						color = Color.FromArgb(128, Color.Black);
				}

				this.radarData[this.radarSheet.Size.Width * this.ingameUi.World.Map.MapSize.Y * 4 + (y * this.radarSheet.Size.Width + x) * 4] = color.B;
				this.radarData[this.radarSheet.Size.Width * this.ingameUi.World.Map.MapSize.Y * 4 + (y * this.radarSheet.Size.Width + x) * 4 + 1] = color.G;
				this.radarData[this.radarSheet.Size.Width * this.ingameUi.World.Map.MapSize.Y * 4 + (y * this.radarSheet.Size.Width + x) * 4 + 2] = color.R;
				this.radarData[this.radarSheet.Size.Width * this.ingameUi.World.Map.MapSize.Y * 4 + (y * this.radarSheet.Size.Width + x) * 4 + 3] = color.A;
			}
		}

		public override string GetCursor(int2 pos)
		{
			var cell =
				new MPos((pos.X - this.RenderBounds.X) / RadarWidget.Scale, (pos.Y - this.RenderBounds.Y) / RadarWidget.Scale).ToCPos(this.ingameUi.World.Map);

			var worldPixel = this.ingameUi.WorldRenderer.ScreenPxPosition(this.ingameUi.World.Map.CenterOfCell(cell));
			var location = this.ingameUi.WorldRenderer.Viewport.WorldToViewPx(worldPixel);
			var mi = new MouseInput { Location = location, Button = Game.Settings.Game.MouseButtonPreference.Action, Modifiers = Game.GetModifierKeys() };
			var cursor = this.ingameUi.World.OrderGenerator.GetCursor(this.ingameUi.World, cell, worldPixel, mi);

			return cursor ?? "default";
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			var cell =
				new MPos((mi.Location.X - this.RenderBounds.X) / RadarWidget.Scale, (mi.Location.Y - this.RenderBounds.Y) / RadarWidget.Scale).ToCPos(
					this.ingameUi.World.Map
				);

			var pos = this.ingameUi.World.Map.CenterOfCell(cell);

			if (mi.Event is MouseInputEvent.Down or MouseInputEvent.Move && mi.Button == Game.Settings.Game.MouseButtonPreference.Cancel)
				this.ingameUi.WorldRenderer.Viewport.Center(pos);

			if (mi.Event != MouseInputEvent.Down || mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
				return true;

			var location = this.ingameUi.WorldRenderer.Viewport.WorldToViewPx(this.ingameUi.WorldRenderer.ScreenPxPosition(pos));

			var fakemi = new MouseInput
			{
				Event = MouseInputEvent.Down, Button = Game.Settings.Game.MouseButtonPreference.Action, Modifiers = mi.Modifiers, Location = location
			};

			var controller = Ui.Root.Get<WorldInteractionControllerWidget>("INTERACTION_CONTROLLER");
			controller.HandleMouseInput(fakemi);
			fakemi.Event = MouseInputEvent.Up;
			controller.HandleMouseInput(fakemi);

			return true;
		}

		public override void Draw()
		{
			this.UpdateShroud();

			WidgetUtils.FillRectWithColor(
				new(
					this.RenderBounds.X - RadarWidget.Scale,
					this.RenderBounds.Y - RadarWidget.Scale,
					this.RenderBounds.Width + RadarWidget.Scale * 2,
					this.RenderBounds.Height + RadarWidget.Scale * 2
				),
				Color.White
			);

			this.radarSheet.CommitBufferedData();

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(this.terrainSprite, new int2(this.RenderBounds.X, this.RenderBounds.Y), RadarWidget.Scale);
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(this.shroudSprite, new int2(this.RenderBounds.X, this.RenderBounds.Y), RadarWidget.Scale);

			var cells = new List<(CPos, Color)>();

			foreach (var e in this.ingameUi.World.ActorsWithTrait<IRadarSignature>())
			{
				if (!e.Actor.IsInWorld
					|| e.Actor.IsDead
					|| this.ingameUi.World.ShroudObscures(e.Actor.CenterPosition)
					|| this.ingameUi.World.FogObscures(e.Actor)
					|| e.Actor.Owner == null)
					continue;

				if (!this.ShowStances.HasRelationship(PlayerRelationship.Ally)
					&& e.Actor.Owner.RelationshipWith(this.ingameUi.World.LocalPlayer).HasRelationship(PlayerRelationship.Ally))
					continue;

				if (!this.ShowStances.HasRelationship(PlayerRelationship.Enemy)
					&& e.Actor.Owner.RelationshipWith(this.ingameUi.World.LocalPlayer).HasRelationship(PlayerRelationship.Enemy))
					continue;

				cells.Clear();
				e.Trait.PopulateRadarSignatureCells(e.Actor, cells);

				foreach (var cell in cells.Select(c => c.Item1))
				{
					if (!this.ingameUi.World.Map.Contains(cell))
						continue;

					var pos = cell.ToMPos(this.ingameUi.World.Map.Grid.Type);
					var color = this.useStanceColor ? Color.FromArgb(e.Actor.Owner.PlayerRelationshipColor(e.Actor).ToArgb()) : e.Actor.Owner.Color;

					WidgetUtils.FillRectWithColor(
						new(
							this.RenderBounds.X + pos.U * RadarWidget.Scale,
							this.RenderBounds.Y + pos.V * RadarWidget.Scale,
							RadarWidget.Scale,
							RadarWidget.Scale
						),
						color
					);
				}
			}

			Game.Renderer.EnableScissor(this.RenderBounds);

			Game.Renderer.RgbaColorRenderer.DrawRect(
				new int2(this.RenderBounds.X, this.RenderBounds.Y) + this.ingameUi.WorldRenderer.Viewport.TopLeft / 32 * RadarWidget.Scale,
				new int2(this.RenderBounds.X, this.RenderBounds.Y) + this.ingameUi.WorldRenderer.Viewport.BottomRight / 32 * RadarWidget.Scale,
				RadarWidget.Scale,
				Color.White
			);

			foreach (var ping in this.ingameUi.RadarPings.Pings)
			{
				if (!ping.IsVisible())
					continue;

				var center = this.ingameUi.World.Map.CellContaining(ping.Position).ToMPos(this.ingameUi.World.Map.Grid.Type);

				var points = ping.Points(new int2(this.RenderBounds.X + center.U * RadarWidget.Scale, this.RenderBounds.Y + center.V * RadarWidget.Scale))
					.ToArray();

				Game.Renderer.RgbaColorRenderer.DrawPolygon(points, 2, ping.Color);
			}

			Game.Renderer.DisableScissor();
		}
	}
}

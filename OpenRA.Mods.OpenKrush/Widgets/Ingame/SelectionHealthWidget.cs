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

using System;
using System.Collections.Generic;
using System.Linq;
using Common.Traits;
using Common.Traits.Render;
using Common.Widgets;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;
using Primitives;

public sealed class SelectionHealthWidget : Widget
{
	private readonly IngameUiWidget ingameUi;
	private readonly World world;

	private Actor[] selectedActors = Array.Empty<Actor>();
	private int selectionHash;

	private readonly Dictionary<string, Sprite> iconCache = new();

	private const int CellWidth = 48;
	private const int Columns = 5;
	private const int BarHeight = 6;
	private const int BarMargin = 2;
	private const int BorderWidth = 1;

	private int lastCols;
	private int lastRows;
	private int lastTotalWidth;
	private int lastTotalHeight;
	private int lastX;
	private int lastY;

	public SelectionHealthWidget(IngameUiWidget ingameUi)
	{
		this.ingameUi = ingameUi;
		this.world = ingameUi.World;
		this.Bounds = new(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);
	}

	public override void Tick()
	{
		if (this.selectionHash == this.world.Selection.Hash)
			return;

		this.selectedActors = this.world.Selection.Actors
			.Where(a => a.Owner == this.world.LocalPlayer && a.IsInWorld && !a.IsDead)
			.ToArray();

		this.selectionHash = this.world.Selection.Hash;

		UpdateLayout();
	}

	private void UpdateLayout()
	{
		if (this.selectedActors.Length == 0)
		{
			this.lastTotalWidth = 0;
			return;
		}

		var visibleCount = System.Math.Min(this.selectedActors.Length, Columns * 2);
		this.lastCols = System.Math.Min(Columns, visibleCount);
		this.lastRows = (visibleCount + this.lastCols - 1) / this.lastCols;
		this.lastTotalWidth = this.lastCols * CellWidth;
		this.lastTotalHeight = this.lastRows * CellWidth;
		this.lastX = (Game.Renderer.Resolution.Width - this.lastTotalWidth) / 2;
		this.lastY = Game.Renderer.Resolution.Height - 56 - this.lastTotalHeight;
	}

	public override bool HandleMouseInput(MouseInput mi)
	{
		if (this.selectedActors.Length == 0 || mi.Event != MouseInputEvent.Down)
			return false;

		var pos = mi.Location;
		if (pos.X < this.lastX - BorderWidth || pos.X > this.lastX + this.lastTotalWidth + BorderWidth
			|| pos.Y < this.lastY - BorderWidth || pos.Y > this.lastY + this.lastTotalHeight + BorderWidth)
			return false;

		if (mi.Button != MouseButton.Left)
			return false;

		for (var i = 0; i < this.lastCols * this.lastRows && i < this.selectedActors.Length; i++)
		{
			var col = i % this.lastCols;
			var row = i / this.lastCols;
			var cellX = this.lastX + col * CellWidth;
			var cellY = this.lastY + row * CellWidth;

			if (pos.X >= cellX && pos.X < cellX + CellWidth && pos.Y >= cellY && pos.Y < cellY + CellWidth)
			{
				var actor = this.selectedActors[i];
				if (!actor.IsInWorld || actor.IsDead)
					return false;

				var shift = Game.GetModifierKeys().HasModifier(Modifiers.Shift);
				var ctrl = Game.GetModifierKeys().HasModifier(Modifiers.Ctrl);

				if (ctrl)
					this.world.Selection.Remove(actor);
				else if (shift)
					this.world.Selection.Add(actor);
				else
					this.world.Selection.Combine(this.world, new[] { actor }, false, true);

				Game.Sound.PlayNotification(this.world.Map.Rules, null, "Sounds", "ClickSound", null);
				return true;
			}
		}

		return false;
	}

	public override void Draw()
	{
		var actors = this.selectedActors
			.Where(a => a.IsInWorld && !a.IsDead)
			.ToArray();

		if (actors.Length == 0 || this.lastTotalWidth == 0)
			return;

		var visibleCount = System.Math.Min(actors.Length, this.lastCols * this.lastRows);
		var cols = this.lastCols;
		var rows = this.lastRows;
		var totalWidth = this.lastTotalWidth;
		var totalHeight = this.lastTotalHeight;
		var x = this.lastX;
		var y = this.lastY;

		// Black backdrop with border
		var bgRect = new Rectangle(x - BorderWidth, y - BorderWidth, totalWidth + BorderWidth * 2, totalHeight + BorderWidth * 2);
		WidgetUtils.FillRectWithColor(bgRect, Color.FromArgb(200, 0, 0, 0));
		WidgetUtils.FillRectWithColor(
			new Rectangle(bgRect.X, bgRect.Y, bgRect.Width, BorderWidth),
			Color.FromArgb(128, 255, 255, 255));
		WidgetUtils.FillRectWithColor(
			new Rectangle(bgRect.X, bgRect.Bottom - BorderWidth, bgRect.Width, BorderWidth),
			Color.FromArgb(128, 255, 255, 255));
		WidgetUtils.FillRectWithColor(
			new Rectangle(bgRect.X, bgRect.Y, BorderWidth, bgRect.Height),
			Color.FromArgb(128, 255, 255, 255));
		WidgetUtils.FillRectWithColor(
			new Rectangle(bgRect.Right - BorderWidth, bgRect.Y, BorderWidth, bgRect.Height),
			Color.FromArgb(128, 255, 255, 255));

		for (var i = 0; i < visibleCount; i++)
		{
			var actor = actors[i];
			var health = actor.TraitOrDefault<Health>();
			if (health == null)
				continue;

			var col = i % cols;
			var row = i / cols;

			var cellX = x + col * CellWidth;
			var cellY = y + row * CellWidth;

			var hpFraction = (float)health.HP / health.MaxHP;
			var hpColor = hpFraction > 0.6f ? Color.FromArgb(200, 0, 200, 0) :
				hpFraction > 0.3f ? Color.FromArgb(200, 200, 200, 0) :
				Color.FromArgb(200, 200, 0, 0);

			var barWidth = CellWidth - BarMargin * 2;
			var barX = cellX + BarMargin;
			var barY = cellY + BarMargin;

			WidgetUtils.FillRectWithColor(
				new Rectangle(barX, barY, barWidth, BarHeight),
				Color.FromArgb(160, 0, 0, 0));

			var fillWidth = System.Math.Max(1, (int)(barWidth * hpFraction));
			WidgetUtils.FillRectWithColor(
				new Rectangle(barX, barY, fillWidth, BarHeight),
				hpColor);

			var icon = GetIcon(actor.Info);
			if (icon != null)
				WidgetUtils.DrawSpriteCentered(
					icon,
					this.ingameUi.Palette,
					new int2(cellX + CellWidth / 2, cellY + CellWidth - 12));
		}

		if (actors.Length > visibleCount)
		{
			var font = Game.Renderer.Fonts["Tiny"];
			font.DrawTextWithContrast(
				$"+{actors.Length - visibleCount}",
				new int2(x + totalWidth / 2 - 15, y + totalHeight + 4),
				Color.White,
				Color.Black,
				1);
		}
	}

	private Sprite? GetIcon(ActorInfo info)
	{
		if (this.iconCache.TryGetValue(info.Name, out var cached))
			return cached;

		var imageName = info.TraitInfoOrDefault<RenderSpritesInfo>()?.Image ?? info.Name;

		try
		{
			var anim = new Animation(this.world, imageName);
			if (anim.HasSequence("icon"))
			{
				anim.PlayFetchIndex("icon", () => 0);
				var sprite = anim.Image;
				if (sprite != null)
				{
					this.iconCache[info.Name] = sprite;
					return sprite;
				}
			}
		}
		catch
		{
		}

		this.iconCache[info.Name] = null!;
		return null;
	}
}

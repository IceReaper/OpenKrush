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

namespace OpenRA.Mods.OpenKrush.Mechanics.Ui.Graphics
{
	using Common.Traits;
	using Oil;
	using OpenRA.Graphics;
	using OpenRA.Traits;
	using Primitives;
	using Researching.Traits;
	using Saboteurs.Traits;
	using System;
	using Traits;
	using Veterancy.Traits;

	public class StatusBar : IRenderable, IFinalizedRenderable
	{
		private readonly Actor actor;
		private readonly AdvancedSelectionDecorationsInfo info;

		private readonly Health? health;
		private readonly SaboteurConquerable? saboteurs;
		private readonly SaboteurConquerableInfo? saboteursInfo;
		private readonly IHaveOil? oil;
		private readonly Researchable? researchable;
		private readonly Veterancy? veteran;
		private readonly VeterancyInfo? veteranInfo;

		public StatusBar(Actor actor, AdvancedSelectionDecorationsInfo info)
		{
			this.actor = actor;
			this.info = info;

			var isAlly = actor.Owner.IsAlliedWith(actor.World.LocalPlayer);

			this.health = actor.TraitOrDefault<Health>();
			this.saboteurs = isAlly ? actor.TraitOrDefault<SaboteurConquerable>() : null;
			this.saboteursInfo = actor.Info.TraitInfoOrDefault<SaboteurConquerableInfo>();
			this.oil = actor.TraitOrDefault<IHaveOil>();
			this.researchable = isAlly ? actor.TraitOrDefault<Researchable>() : null;
			this.veteran = actor.TraitOrDefault<Veterancy>();
			this.veteranInfo = actor.Info.TraitInfoOrDefault<VeterancyInfo>();
		}

		public WPos Pos => WPos.Zero;
		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset)
		{
			return this;
		}

		public IRenderable OffsetBy(in WVec offset)
		{
			return this;
		}

		public IRenderable AsDecoration()
		{
			return this;
		}

		public IFinalizedRenderable PrepareRender(WorldRenderer wr)
		{
			return this;
		}

		public void Render(WorldRenderer wr)
		{
			if (this.health == null && this.saboteurs == null && this.oil == null && this.researchable == null)
				return;

			var bounds = this.actor.TraitOrDefault<IMouseBounds>().MouseoverBounds(this.actor, wr).BoundingRect;

			var thickness = this.info.BigVariant ? 4 : 3;

			var height = (this.health != null ? thickness : 0)
				+ (this.saboteurs != null ? thickness : 0)
				+ (this.researchable != null ? thickness : 0)
				+ (this.oil != null ? thickness : 0)
				- 1;

			var width = this.info.Width == 0 ? bounds.Width : this.info.Width;

			this.DrawRect(
				wr,
				bounds,
				0,
				-height - 4,
				width,
				height + 4,
				this.veteran is { Level: > 0 } && this.veteranInfo != null
					? this.veteranInfo.Levels[this.veteran.Level - 1]
					: Color.FromArgb(255, 206, 206, 206)
			);

			this.DrawRect(wr, bounds, 1, -height - 3, width - 2, height + 2, Color.FromArgb(255, 16, 16, 16));

			var current = 0;

			if (this.health != null)
			{
				var progress = (width - 4) * this.health.HP / this.health.MaxHP;

				this.DrawRect(wr, bounds, 2, -height - 2, width - 4, 1, Color.FromArgb(255, 206, 206, 206));
				this.DrawRect(wr, bounds, 2, -height - 1, width - 4, thickness - 2, Color.FromArgb(255, 49, 49, 49));

				switch (this.health.DamageState)
				{
					case DamageState.Undamaged:
						this.DrawRect(wr, bounds, 2, -height - 2, progress, 1, Color.FromArgb(255, 0, 255, 0));
						this.DrawRect(wr, bounds, 2, -height - 1, progress, thickness - 2, Color.FromArgb(255, 0, 181, 0));

						break;

					case DamageState.Light:
						this.DrawRect(wr, bounds, 2, -height - 2, progress, 1, Color.FromArgb(255, 255, 255, 0));
						this.DrawRect(wr, bounds, 2, -height - 1, progress, thickness - 2, Color.FromArgb(255, 141, 184, 28));

						break;

					case DamageState.Medium:
						this.DrawRect(wr, bounds, 2, -height - 2, progress, 1, Color.FromArgb(255, 255, 156, 0));
						this.DrawRect(wr, bounds, 2, -height - 1, progress, thickness - 2, Color.FromArgb(255, 178, 122, 51));

						break;

					case DamageState.Heavy:
						this.DrawRect(wr, bounds, 2, -height - 2, progress, 1, Color.FromArgb(255, 230, 0, 0));
						this.DrawRect(wr, bounds, 2, -height - 1, progress, thickness - 2, Color.FromArgb(255, 123, 0, 0));

						break;

					case DamageState.Critical:
						this.DrawRect(wr, bounds, 2, -height - 2, progress, 1, Color.FromArgb(255, 123, 0, 0));
						this.DrawRect(wr, bounds, 2, -height - 1, progress, thickness - 2, Color.FromArgb(255, 82, 0, 0));

						break;

					case DamageState.Dead:
						break;

					default:
						throw new ArgumentOutOfRangeException(Enum.GetName(this.health.DamageState));
				}

				current += thickness;
			}

			if (this.saboteurs != null && this.saboteursInfo != null)
			{
				var progress = (width - 4) * this.saboteurs.Population / this.saboteursInfo.MaxPopulation;

				this.DrawRect(wr, bounds, 2, -height - 2 + current, width - 4, 1, Color.FromArgb(255, 206, 206, 206));
				this.DrawRect(wr, bounds, 2, -height - 1 + current, width - 4, thickness - 2, Color.FromArgb(255, 49, 49, 49));

				this.DrawRect(wr, bounds, 2, -height - 2 + current, progress, 1, Color.FromArgb(255, 230, 0, 0));
				this.DrawRect(wr, bounds, 2, -height - 1 + current, progress, thickness - 2, Color.FromArgb(255, 123, 0, 0));

				for (var i = 1; i < this.saboteursInfo.MaxPopulation; i++)
				{
					this.DrawRect(
						wr,
						bounds,
						2 + (width - 4) * i / this.saboteursInfo.MaxPopulation,
						-height - 2 + current,
						1,
						thickness - 1,
						Color.FromArgb(255, 16, 16, 16)
					);
				}

				current += thickness;
			}

			if (this.researchable is { MaxLevel: > 0 })
			{
				var segments = this.researchable.MaxLevel + this.researchable.LimitedLevels;
				var progress = (width - 4) * this.researchable.Level / segments;
				var unavailable = (width - 4) * this.researchable.LimitedLevels / segments;

				this.DrawRect(wr, bounds, 2, -height - 2 + current, width - 4, 1, Color.FromArgb(255, 206, 206, 206));
				this.DrawRect(wr, bounds, 2, -height - 1 + current, width - 4, thickness - 2, Color.FromArgb(255, 49, 49, 49));

				this.DrawRect(wr, bounds, 2, -height - 2 + current, progress, 1, Color.FromArgb(255, 0, 165, 255));
				this.DrawRect(wr, bounds, 2, -height - 1 + current, progress, thickness - 2, Color.FromArgb(255, 0, 66, 255));

				this.DrawRect(wr, bounds, width - 2 - unavailable, -height - 2 + current, unavailable, thickness, Color.FromArgb(255, 16, 16, 16));

				for (var i = 1; i < segments; i++)
					this.DrawRect(wr, bounds, 2 + (width - 4) * i / segments, -height - 2 + current, 1, thickness - 1, Color.FromArgb(255, 16, 16, 16));

				current += thickness;
			}

			if (this.oil == null)
				return;

			var oilProgress = (width - 4) * this.oil.Current / this.oil.Maximum;

			this.DrawRect(wr, bounds, 2, -height - 2 + current, width - 4, 1, Color.FromArgb(255, 206, 206, 206));
			this.DrawRect(wr, bounds, 2, -height - 1 + current, width - 4, thickness - 2, Color.FromArgb(255, 49, 49, 49));

			this.DrawRect(wr, bounds, 2, -height - 2 + current, oilProgress, 1, Color.FromArgb(255, 0, 165, 255));
			this.DrawRect(wr, bounds, 2, -height - 1 + current, oilProgress, thickness - 2, Color.FromArgb(255, 0, 66, 255));
		}

		private void DrawRect(WorldRenderer wr, Rectangle bounds, int x, int y, int w, int h, Color c)
		{
			var renderPosition = wr.Viewport.WorldToViewPx(bounds.Location);

			var width = this.info.Width == 0 ? bounds.Width : this.info.Width;
			var center = (bounds.Width - width) / 2;

			Game.Renderer.RgbaColorRenderer.FillRect(
				new(renderPosition.X + (x + this.info.Offset.X + center) * wr.Viewport.Zoom, renderPosition.Y + (y + this.info.Offset.Y) * wr.Viewport.Zoom, 0),
				new(
					renderPosition.X + (x + this.info.Offset.X + center + w) * wr.Viewport.Zoom,
					renderPosition.Y + (y + this.info.Offset.Y + h) * wr.Viewport.Zoom,
					0
				),
				c
			);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
		}

		public Rectangle ScreenBounds(WorldRenderer wr)
		{
			return Rectangle.Empty;
		}
	}
}

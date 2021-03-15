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

namespace OpenRA.Mods.OpenKrush.Graphics
{
	using System;
	using Common.Traits;
	using Mechanics.Oil;
	using Mechanics.Researching.Traits;
	using Mechanics.Saboteurs.Traits;
	using Mechanics.Veterancy.Traits;
	using OpenRA.Graphics;
	using OpenRA.Traits;
	using Primitives;
	using Traits.Render;

	public class StatusBar : IRenderable, IFinalizedRenderable
	{
		private Actor actor;
		private AdvancedSelectionDecorationsInfo info;

		private Health health;
		private SaboteurConquerable saboteurs;
		private SaboteurConquerableInfo saboteursInfo;
		private IHaveOil oil;
		private Researchable researchable;
		private ResearchableInfo researchableInfo;
		private TechLevel techLevel;
		private Veterancy veteran;
		private VeterancyInfo veteranInfo;

		public StatusBar(Actor actor, AdvancedSelectionDecorationsInfo info)
		{
			this.actor = actor;
			this.info = info;

			var isAlly = actor.Owner.IsAlliedWith(actor.World.LocalPlayer);

			health = actor.TraitOrDefault<Health>();
			saboteurs = isAlly ? actor.TraitOrDefault<SaboteurConquerable>() : null;
			saboteursInfo = actor.Info.TraitInfoOrDefault<SaboteurConquerableInfo>();
			oil = actor.TraitOrDefault<IHaveOil>();
			researchable = isAlly ? actor.TraitOrDefault<Researchable>() : null;
			researchableInfo = actor.Info.TraitInfoOrDefault<ResearchableInfo>();
			techLevel = actor.World.WorldActor.Trait<TechLevel>();
			veteran = actor.TraitOrDefault<Veterancy>();
			veteranInfo = actor.Info.TraitInfoOrDefault<VeterancyInfo>();
		}

		public WPos Pos => WPos.Zero;
		public PaletteReference Palette => null;
		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithPalette(PaletteReference newPalette)
		{
			return this;
		}

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
			if (health == null && saboteurs == null && oil == null && researchable == null)
				return;

			var bounds = actor.Trait<IMouseBounds>().MouseoverBounds(actor, wr).BoundingRect;

			var thickness = info.BigVariant ? 4 : 3;

			var height = (health != null ? thickness : 0)
				+ (saboteurs != null ? thickness : 0)
				+ (researchable != null ? thickness : 0)
				+ (oil != null ? thickness : 0)
				- 1;

			var width = info.Width == 0 ? bounds.Width : info.Width;

			DrawRect(
				wr,
				bounds,
				0,
				-height - 4,
				width,
				height + 4,
				veteran != null && veteran.Level > 0 ? veteranInfo.Levels[veteran.Level - 1] : Color.FromArgb(255, 206, 206, 206));

			DrawRect(wr, bounds, 1, -height - 3, width - 2, height + 2, Color.FromArgb(255, 16, 16, 16));

			var current = 0;

			if (health != null)
			{
				var progress = (width - 4) * health.HP / health.MaxHP;

				DrawRect(wr, bounds, 2, -height - 2 + current * thickness, width - 4, 1, Color.FromArgb(255, 206, 206, 206));
				DrawRect(wr, bounds, 2, -height - 1 + current * thickness, width - 4, thickness - 2, Color.FromArgb(255, 49, 49, 49));

				switch (health.DamageState)
				{
					case DamageState.Undamaged:
						DrawRect(wr, bounds, 2, -height - 2 + current * thickness, progress, 1, Color.FromArgb(255, 0, 255, 0));
						DrawRect(wr, bounds, 2, -height - 1 + current * thickness, progress, thickness - 2, Color.FromArgb(255, 0, 181, 0));

						break;

					case DamageState.Light:
						DrawRect(wr, bounds, 2, -height - 2 + current * thickness, progress, 1, Color.FromArgb(255, 255, 255, 0));
						DrawRect(wr, bounds, 2, -height - 1 + current * thickness, progress, thickness - 2, Color.FromArgb(255, 141, 184, 28));

						break;

					case DamageState.Medium:
						DrawRect(wr, bounds, 2, -height - 2 + current * thickness, progress, 1, Color.FromArgb(255, 255, 156, 0));
						DrawRect(wr, bounds, 2, -height - 1 + current * thickness, progress, thickness - 2, Color.FromArgb(255, 178, 122, 51));

						break;

					case DamageState.Heavy:
						DrawRect(wr, bounds, 2, -height - 2 + current * thickness, progress, 1, Color.FromArgb(255, 230, 0, 0));
						DrawRect(wr, bounds, 2, -height - 1 + current * thickness, progress, thickness - 2, Color.FromArgb(255, 123, 0, 0));

						break;

					case DamageState.Critical:
						DrawRect(wr, bounds, 2, -height - 2 + current * thickness, progress, 1, Color.FromArgb(255, 123, 0, 0));
						DrawRect(wr, bounds, 2, -height - 1 + current * thickness, progress, thickness - 2, Color.FromArgb(255, 82, 0, 0));

						break;
				}

				current++;
			}

			if (saboteurs != null)
			{
				var progress = (width - 4) * saboteurs.Population / saboteursInfo.MaxPopulation;

				DrawRect(wr, bounds, 2, -height - 2 + current * thickness, width - 4, 1, Color.FromArgb(255, 206, 206, 206));
				DrawRect(wr, bounds, 2, -height - 1 + current * thickness, width - 4, thickness - 2, Color.FromArgb(255, 49, 49, 49));

				DrawRect(wr, bounds, 2, -height - 2 + current * thickness, progress, 1, Color.FromArgb(255, 230, 0, 0));
				DrawRect(wr, bounds, 2, -height - 1 + current * thickness, progress, thickness - 2, Color.FromArgb(255, 123, 0, 0));

				for (var i = 1; i < saboteursInfo.MaxPopulation; i++)
					DrawRect(
						wr,
						bounds,
						2 + (width - 4) * i / saboteursInfo.MaxPopulation,
						-height - 2 + current * thickness,
						1,
						thickness - 1,
						Color.FromArgb(255, 16, 16, 16));

				current++;
			}

			if (researchable != null)
			{
				var progress = (width - 4) * researchable.Level / researchableInfo.MaxLevel;
				var unavailable = (width - 4) * Math.Max(0, researchableInfo.MaxLevel - techLevel.TechLevels) / researchableInfo.MaxLevel;

				DrawRect(wr, bounds, 2, -height - 2 + current * thickness, width - 4, 1, Color.FromArgb(255, 206, 206, 206));
				DrawRect(wr, bounds, 2, -height - 1 + current * thickness, width - 4, thickness - 2, Color.FromArgb(255, 49, 49, 49));

				DrawRect(wr, bounds, 2, -height - 2 + current * thickness, progress, 1, Color.FromArgb(255, 0, 165, 255));
				DrawRect(wr, bounds, 2, -height - 1 + current * thickness, progress, thickness - 2, Color.FromArgb(255, 0, 66, 255));

				DrawRect(wr, bounds, width - 2 - unavailable, -height - 2 + current * thickness, unavailable, thickness, Color.FromArgb(255, 16, 16, 16));

				for (var i = 1; i < researchableInfo.MaxLevel; i++)
					DrawRect(
						wr,
						bounds,
						2 + (width - 4) * i / researchableInfo.MaxLevel,
						-height - 2 + current * thickness,
						1,
						thickness - 1,
						Color.FromArgb(255, 16, 16, 16));

				current++;
			}

			if (oil != null)
			{
				var progress = (width - 4) * oil.Current / oil.Maximum;

				DrawRect(wr, bounds, 2, -height - 2 + current * thickness, width - 4, 1, Color.FromArgb(255, 206, 206, 206));
				DrawRect(wr, bounds, 2, -height - 1 + current * thickness, width - 4, thickness - 2, Color.FromArgb(255, 49, 49, 49));

				DrawRect(wr, bounds, 2, -height - 2 + current * thickness, progress, 1, Color.FromArgb(255, 0, 165, 255));
				DrawRect(wr, bounds, 2, -height - 1 + current * thickness, progress, thickness - 2, Color.FromArgb(255, 0, 66, 255));
			}
		}

		private void DrawRect(WorldRenderer wr, Rectangle bounds, int x, int y, int w, int h, Color c)
		{
			var renderPosition = wr.Viewport.WorldToViewPx(bounds.Location);

			var width = info.Width == 0 ? bounds.Width : info.Width;
			var center = (bounds.Width - width) / 2;

			Game.Renderer.RgbaColorRenderer.FillRect(
				new float3(renderPosition.X + (x + info.Offset.X + center) * wr.Viewport.Zoom, renderPosition.Y + (y + info.Offset.Y) * wr.Viewport.Zoom, 0),
				new float3(
					renderPosition.X + (x + info.Offset.X + center + w) * wr.Viewport.Zoom,
					renderPosition.Y + (y + info.Offset.Y + h) * wr.Viewport.Zoom,
					0),
				c);
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

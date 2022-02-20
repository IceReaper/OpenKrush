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

namespace OpenRA.Mods.OpenKrush.Widgets.Ingame.Buttons
{
	using Common;
	using Common.Traits;
	using Common.Widgets;
	using Graphics;
	using Primitives;

	public class ProductionItemButtonWidget : SidebarButtonWidget
	{
		public string? Item;
		public string? Icon;
		public Action<MouseInput>? ClickedLeft;
		public Action<MouseInput>? ClickedRight;
		public Func<bool>? IsActive;
		public Func<bool>? IsFocused;
		public Func<int>? Progress;
		public Func<int>? Amount;

		private ActorPreviewWidget? actorPreviewWidget;
		private bool isHovered;
		private bool initialized;
		private Sprite? image;

		public ProductionItemButtonWidget(SidebarWidget sidebar)
			: base(sidebar, "unit")
		{
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			this.ClickedLeft?.Invoke(mi);

			return true;
		}

		protected override bool HandleRightClick(MouseInput mi)
		{
			this.ClickedRight?.Invoke(mi);

			return true;
		}

		public override void MouseEntered()
		{
			base.MouseEntered();
			this.isHovered = true;
		}

		public override void MouseExited()
		{
			base.MouseExited();
			this.isHovered = false;
		}

		public override void Tick()
		{
			this.Active = this.IsActive?.Invoke() ?? false;

			if (this.actorPreviewWidget != null && this.isHovered)
				this.actorPreviewWidget.Tick();
		}

		public override void Draw()
		{
			base.Draw();

			if (this.Progress == null)
				return;

			var progress = this.Progress();

			if (progress == -1)
				return;

			progress = progress * (SidebarButtonWidget.Size - 10) / 100;
			var o = SidebarButtonWidget.Size - 10 - progress;
			WidgetUtils.FillRectWithColor(new(this.RenderBounds.X + 2, this.RenderBounds.Y + 4, 7, SidebarButtonWidget.Size - 6), Color.Black);

			WidgetUtils.FillRectWithColor(
				new(this.RenderBounds.X + 3, this.RenderBounds.Y + 5, 5, SidebarButtonWidget.Size - 8),
				this.Sidebar.IngameUi.Palette.Palette.GetColor(10)
			);

			WidgetUtils.FillRectWithColor(
				new(this.RenderBounds.X + 4, this.RenderBounds.Y + 6, 3, SidebarButtonWidget.Size - 10),
				this.Sidebar.IngameUi.Palette.Palette.GetColor(8)
			);

			WidgetUtils.FillRectWithColor(
				new(this.RenderBounds.X + 4, this.RenderBounds.Y + 6 + o, 3, progress),
				this.Sidebar.IngameUi.Palette.Palette.GetColor(12)
			);

			var amount = this.Amount?.Invoke() ?? 0;

			switch (amount)
			{
				case -1:
					this.Sidebar.Font.PlayFetchIndex("production", () => 10);

					WidgetUtils.DrawSpriteCentered(
						this.Sidebar.Font.Image,
						this.Sidebar.IngameUi.Palette,
						new int2(this.RenderBounds.X + 14 + 4, this.RenderBounds.Y + 40)
					);

					break;

				case > 1:
				{
					var numberString = amount.ToString();

					for (var i = 0; i < numberString.Length; i++)
					{
						var j = i;
						this.Sidebar.Font.PlayFetchIndex("production", () => numberString[j] - 0x30);

						WidgetUtils.DrawSpriteCentered(
							this.Sidebar.Font.Image,
							this.Sidebar.IngameUi.Palette,
							new int2(this.RenderBounds.X + 14 + i * 8, this.RenderBounds.Y + 40)
						);
					}

					break;
				}
			}
		}

		public override void PrepareRenderables()
		{
			if (!this.initialized)
			{
				this.initialized = true;

				var actor = new Animation(this.Sidebar.IngameUi.World, this.Icon);

				if (actor.HasSequence("icon"))
				{
					actor.PlayFetchIndex("icon", () => 0);
					this.image = actor.Image;

					return;
				}

				if (this.actorPreviewWidget == null)
				{
					this.actorPreviewWidget = new(this.Sidebar.IngameUi.WorldRenderer) { Animate = true };

					this.actorPreviewWidget.SetPreview(
						this.Sidebar.IngameUi.World.Map.Rules.Actors[this.Item],
						new()
						{
							new FacingInit(WAngle.FromFacing(96)),
							new TurretFacingInit(WAngle.FromFacing(96)),
							new OwnerInit(this.Sidebar.IngameUi.World.LocalPlayer),
							new FactionInit(this.Sidebar.IngameUi.World.LocalPlayer.Faction.Name)
						}
					);

					/*
					// TODO implement per actor offsets
					// TODO implement per ui inner regions
					// TODO fix turret palettes!
					*/

					var factorX = this.Sidebar.ButtonArea.Width / (float)this.actorPreviewWidget.IdealPreviewSize.X;
					var factorY = this.Sidebar.ButtonArea.Height / (float)this.actorPreviewWidget.IdealPreviewSize.Y;

					if (factorX <= 1 && factorY <= 1)
					{
						var factor = Math.Max(factorX, factorY);
						this.actorPreviewWidget.GetScale = () => factor;
					}
				}
			}

			this.actorPreviewWidget?.PrepareRenderables();
		}

		protected override void DrawContents()
		{
			if (this.IsFocused?.Invoke() ?? false)
				WidgetUtils.FillRectWithColor(this.RenderBounds, Color.FromArgb(25, 255, 255, 255));

			if (this.image != null)
				WidgetUtils.DrawSpriteCentered(this.image, this.Sidebar.IngameUi.Palette, this.Center + new int2(0, this.Active ? 1 : 0));
			else
			{
				var previewWidget = this.actorPreviewWidget;

				if (previewWidget != null)
				{
					previewWidget.Bounds = this.RenderBounds;

					Game.Renderer.EnableScissor(
						new(
							this.RenderBounds.X + this.Sidebar.ButtonArea.X,
							this.RenderBounds.Y + this.Sidebar.ButtonArea.Y,
							this.Sidebar.ButtonArea.Width,
							this.Sidebar.ButtonArea.Height
						)
					);

					previewWidget.Draw();
				}

				Game.Renderer.DisableScissor();
			}
		}
	}
}

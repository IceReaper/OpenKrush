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
	using System.Linq;
	using Buttons;
	using Common.Widgets;
	using OpenRA.Graphics;
	using OpenRA.Widgets;
	using Primitives;
	using ButtonWidget = Buttons.ButtonWidget;

	public sealed class SidebarWidget : Widget
	{
		public const string Identifier = "OPENKRUSH_SIDEBAR";
		public readonly IngameUiWidget IngameUi;

		public readonly Animation Buttons;
		public readonly Animation Font;
		public readonly Rectangle ButtonArea;

		private BomberButtonWidget bomber;
		private SellButtonWidget sell;
		private ResearchButtonWidget research;
		private RepairButtonWidget repair;
		private RadarButtonWidget radar;
		private OptionsButtonWidget options;

		public SidebarWidget(IngameUiWidget ingameUi)
		{
			IngameUi = ingameUi;
			Id = SidebarWidget.Identifier;

			Buttons = new Animation(IngameUi.World, $"sidebar-{IngameUi.World.LocalPlayer.Faction.InternalName}");
			Font = new Animation(IngameUi.World, "font");

			ChromeMetrics.TryGet($"ButtonArea-{IngameUi.World.LocalPlayer.Faction.InternalName}", out ButtonArea);

			AddChild(new ProductionCategoryButtonWidget(this, 0, new[] { "infantry" }, "Infantry"));
			AddChild(new ProductionCategoryButtonWidget(this, 1, new[] { "vehicle", "beast" }, "Vehicles"));
			AddChild(new ProductionCategoryButtonWidget(this, 2, new[] { "building" }, "Buildings"));
			AddChild(new ProductionCategoryButtonWidget(this, 3, new[] { "tower" }, "Towers"));
			AddChild(new ProductionCategoryButtonWidget(this, 4, new[] { "wall" }, "Walls"));

			AddChild(bomber = new BomberButtonWidget(this));

			AddChild(sell = new SellButtonWidget(this));
			AddChild(research = new ResearchButtonWidget(this));
			AddChild(repair = new RepairButtonWidget(this));

			AddChild(radar = new RadarButtonWidget(this));
			AddChild(options = new OptionsButtonWidget(this));

			Resize();
		}

		public void Resize()
		{
			Bounds = new Rectangle(Game.Renderer.Resolution.Width - ButtonWidget.Size, 0, ButtonWidget.Size, Game.Renderer.Resolution.Height);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			return EventBounds.Contains(mi.Location);
		}

		public override void Tick()
		{
			if (Bounds.Height < 14 * ButtonWidget.Size)
			{
				bomber.Bounds.Y = 5 * ButtonWidget.Size;
				sell.Bounds.Y = 6 * ButtonWidget.Size;
				research.Bounds.Y = 7 * ButtonWidget.Size;
				repair.Bounds.Y = 8 * ButtonWidget.Size;
				radar.Bounds.Y = 9 * ButtonWidget.Size;
				options.Bounds.Y = 10 * ButtonWidget.Size;
			}
			else
			{
				bomber.Bounds.Y = 6 * ButtonWidget.Size;
				sell.Bounds.Y = 8 * ButtonWidget.Size;
				research.Bounds.Y = 9 * ButtonWidget.Size;
				repair.Bounds.Y = 10 * ButtonWidget.Size;
				radar.Bounds.Y = 12 * ButtonWidget.Size;
				options.Bounds.Y = 13 * ButtonWidget.Size;
			}
		}

		public override void Draw()
		{
			for (var y = 0; y < Bounds.Height; y += ButtonWidget.Size)
			{
				Buttons.PlayFetchIndex("button", () => 0);
				WidgetUtils.DrawSHPCentered(Buttons.Image, new float2(RenderBounds.X + ButtonWidget.Size / 2, y + ButtonWidget.Size / 2), IngameUi.Palette);
			}
		}

		public void CloseAllBut(ButtonWidget keepOpen)
		{
			foreach (var widget in Children.Where(w => w != keepOpen && (w is ProductionCategoryButtonWidget || w is BomberButtonWidget)))
				((ButtonWidget)widget).Active = false;
		}

		public void SelectFactory(Actor factory, string category)
		{
			var categoryButton = Children.FirstOrDefault(
				child =>
				{
					var button = child as ProductionCategoryButtonWidget;

					if (button == null)
						return false;

					return button.Categories.Contains(category);
				}) as ProductionCategoryButtonWidget;

			if (categoryButton == null)
				return;

			CloseAllBut(categoryButton);
			categoryButton.Active = true;

			categoryButton.SelectFactory(factory);
		}
	}
}

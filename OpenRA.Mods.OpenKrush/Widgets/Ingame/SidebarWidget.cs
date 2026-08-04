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

using System.Linq;
using Buttons;
using Common.Widgets;
using Graphics;
using Mechanics.Researching.Orders;
using OpenRA.Widgets;
using Primitives;

public sealed class SidebarWidget : Widget
{
	public const string Identifier = "OPENKRUSH_SIDEBAR";
	public const int GridColumns = 5;
	public const int GridRows = 3;
	public const int BarHeight = SidebarButtonWidget.Size;
	public const int ButtonCount = 11;

	public static int GridWidth => GridColumns * SidebarButtonWidget.Size;
	public static int GridHeight => GridRows * SidebarButtonWidget.Size;
	public static int BarWidth => ButtonCount * SidebarButtonWidget.Size;

	public readonly IngameUiWidget IngameUi;

	public readonly Animation Buttons;
	public readonly Animation Font;
	public readonly Rectangle ButtonArea;

	private readonly BomberButtonWidget bomber;
	private readonly SellButtonWidget sell;
	private readonly ResearchButtonWidget research;
	private readonly RepairButtonWidget repair;
	private readonly RadarButtonWidget radar;
	private readonly OptionsButtonWidget options;

	public SidebarWidget(IngameUiWidget ingameUi)
	{
		this.IngameUi = ingameUi;
		this.Id = SidebarWidget.Identifier;

		this.Buttons = new(this.IngameUi.World, $"sidebar-{this.IngameUi.World.LocalPlayer.Faction.InternalName}");
		this.Font = new(this.IngameUi.World, "font");

		ChromeMetrics.TryGet($"ButtonArea-{this.IngameUi.World.LocalPlayer.Faction.InternalName}", out this.ButtonArea);

		this.AddChild(this.bomber = new(this));
		this.AddChild(this.sell = new(this));
		this.AddChild(this.research = new(this));
		this.AddChild(this.repair = new(this));
		this.AddChild(this.radar = new(this));
		this.AddChild(this.options = new(this));

		this.AddChild(new ProductionCategoryButtonWidget(this, 0, new[] { "infantry" }, "Infantry"));
		this.AddChild(new ProductionCategoryButtonWidget(this, 1, new[] { "vehicle", "beast" }, "Vehicles"));
		this.AddChild(new ProductionCategoryButtonWidget(this, 2, new[] { "building" }, "Buildings"));
		this.AddChild(new ProductionCategoryButtonWidget(this, 3, new[] { "tower" }, "Towers"));
		this.AddChild(new ProductionCategoryButtonWidget(this, 4, new[] { "wall" }, "Walls"));

		this.AddChild(new UnitCommandWidget(this));

		this.Resize();
	}

	private void Resize()
	{
		this.Bounds = new(
			Game.Renderer.Resolution.Width - SidebarWidget.BarWidth,
			Game.Renderer.Resolution.Height - SidebarWidget.GridHeight - SidebarWidget.BarHeight,
			SidebarWidget.BarWidth,
			SidebarWidget.GridHeight + SidebarWidget.BarHeight
		);
	}

	public bool AnyCategoryOpen => this.Children.OfType<ProductionCategoryButtonWidget>().Any(w => w.Active);

	public override bool HandleMouseInput(MouseInput mi)
	{
		return this.EventBounds.Contains(mi.Location);
	}

	public override void Tick()
	{
		this.bomber.Bounds = new(5 * SidebarButtonWidget.Size, SidebarWidget.GridHeight, SidebarButtonWidget.Size, SidebarButtonWidget.Size);
		this.sell.Bounds = new(6 * SidebarButtonWidget.Size, SidebarWidget.GridHeight, SidebarButtonWidget.Size, SidebarButtonWidget.Size);
		this.research.Bounds = new(7 * SidebarButtonWidget.Size, SidebarWidget.GridHeight, SidebarButtonWidget.Size, SidebarButtonWidget.Size);
		this.repair.Bounds = new(8 * SidebarButtonWidget.Size, SidebarWidget.GridHeight, SidebarButtonWidget.Size, SidebarButtonWidget.Size);
		this.radar.Bounds = new(9 * SidebarButtonWidget.Size, SidebarWidget.GridHeight, SidebarButtonWidget.Size, SidebarButtonWidget.Size);
		this.options.Bounds = new(10 * SidebarButtonWidget.Size, SidebarWidget.GridHeight, SidebarButtonWidget.Size, SidebarButtonWidget.Size);

		// Research, Sell and Repair are now in the building command grid (UnitCommandWidget).
		this.research.Visible = false;
		this.sell.Visible = false;
		this.repair.Visible = false;

		base.Tick();
	}

	public override void Draw()
	{
		WidgetUtils.FillRectWithColor(
			new(this.RenderBounds.X, this.RenderBounds.Y, SidebarWidget.BarWidth, SidebarWidget.GridHeight),
			Color.FromArgb(96, 0, 0, 0)
		);

		for (var x = 0; x < SidebarWidget.BarWidth; x += SidebarButtonWidget.Size)
		{
			this.Buttons.PlayFetchIndex("button", () => 0);

			WidgetUtils.DrawSpriteCentered(
				this.Buttons.Image,
				this.IngameUi.Palette,
				new(this.RenderBounds.X + x + SidebarButtonWidget.Size / 2, this.RenderBounds.Y + SidebarWidget.GridHeight + SidebarButtonWidget.Size / 2)
			);
		}

		base.Draw();
	}

	public void CloseAllBut(SidebarButtonWidget? keepOpen)
	{
		foreach (var widget in this.Children.Where(w => w != keepOpen && w is ProductionCategoryButtonWidget or BomberButtonWidget))
			((SidebarButtonWidget)widget).Active = false;

		if (keepOpen is not ResearchButtonWidget && this.IngameUi.World.OrderGenerator is ResearchOrderGenerator)
			this.IngameUi.World.CancelInputMode();
	}

	public void SelectFactory(Actor factory, string category)
	{
		if (category == "research")
		{
			this.CloseAllBut(null);
			this.IngameUi.World.OrderGenerator = new ResearchOrderGenerator();

			return;
		}

		if (this.Children.FirstOrDefault(child => child is ProductionCategoryButtonWidget button && button.Categories.Contains(category)) is not
			ProductionCategoryButtonWidget categoryButton)
			return;

		this.CloseAllBut(categoryButton);
		categoryButton.Active = true;

		categoryButton.SelectFactory(factory);
	}
}

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

namespace OpenRA.Mods.OpenKrush.Widgets.Ingame;

using Buttons;
using Common.Widgets;
using Graphics;
using OpenRA.Widgets;
using Primitives;

public sealed class SidebarWidget : Widget
{
	public const string Identifier = "OPENKRUSH_SIDEBAR";
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

		this.AddChild(new ProductionCategoryButtonWidget(this, 0, new[] { "infantry" }, "Infantry"));
		this.AddChild(new ProductionCategoryButtonWidget(this, 1, new[] { "vehicle", "beast" }, "Vehicles"));
		this.AddChild(new ProductionCategoryButtonWidget(this, 2, new[] { "building" }, "Buildings"));
		this.AddChild(new ProductionCategoryButtonWidget(this, 3, new[] { "tower" }, "Towers"));
		this.AddChild(new ProductionCategoryButtonWidget(this, 4, new[] { "wall" }, "Walls"));

		this.AddChild(this.bomber = new(this));

		this.AddChild(this.sell = new(this));
		this.AddChild(this.research = new(this));
		this.AddChild(this.repair = new(this));

		this.AddChild(this.radar = new(this));
		this.AddChild(this.options = new(this));

		this.Resize();
	}

	private void Resize()
	{
		this.Bounds = new(Game.Renderer.Resolution.Width - SidebarButtonWidget.Size, 0, SidebarButtonWidget.Size, Game.Renderer.Resolution.Height);
	}

	public override bool HandleMouseInput(MouseInput mi)
	{
		return this.EventBounds.Contains(mi.Location);
	}

	public override void Tick()
	{
		if (this.Bounds.Height < 14 * SidebarButtonWidget.Size)
		{
			this.bomber.Bounds.Y = 5 * SidebarButtonWidget.Size;
			this.sell.Bounds.Y = 6 * SidebarButtonWidget.Size;
			this.research.Bounds.Y = 7 * SidebarButtonWidget.Size;
			this.repair.Bounds.Y = 8 * SidebarButtonWidget.Size;
			this.radar.Bounds.Y = 9 * SidebarButtonWidget.Size;
			this.options.Bounds.Y = 10 * SidebarButtonWidget.Size;
		}
		else
		{
			this.bomber.Bounds.Y = 6 * SidebarButtonWidget.Size;
			this.sell.Bounds.Y = 8 * SidebarButtonWidget.Size;
			this.research.Bounds.Y = 9 * SidebarButtonWidget.Size;
			this.repair.Bounds.Y = 10 * SidebarButtonWidget.Size;
			this.radar.Bounds.Y = 12 * SidebarButtonWidget.Size;
			this.options.Bounds.Y = 13 * SidebarButtonWidget.Size;
		}
	}

	public override void Draw()
	{
		for (var y = 0; y < this.Bounds.Height; y += SidebarButtonWidget.Size)
		{
			this.Buttons.PlayFetchIndex("button", () => 0);

			WidgetUtils.DrawSpriteCentered(
				this.Buttons.Image,
				this.IngameUi.Palette,
				new(this.RenderBounds.X + SidebarButtonWidget.Size / 2, y + SidebarButtonWidget.Size / 2)
			);
		}
	}

	public void CloseAllBut(SidebarButtonWidget keepOpen)
	{
		foreach (var widget in this.Children.Where(w => w != keepOpen && w is ProductionCategoryButtonWidget or BomberButtonWidget))
			((SidebarButtonWidget)widget).Active = false;
	}

	public void SelectFactory(Actor factory, string category)
	{
		if (this.Children.FirstOrDefault(child => child is ProductionCategoryButtonWidget button && button.Categories.Contains(category)) is not
			ProductionCategoryButtonWidget categoryButton)
			return;

		this.CloseAllBut(categoryButton);
		categoryButton.Active = true;

		categoryButton.SelectFactory(factory);
	}
}

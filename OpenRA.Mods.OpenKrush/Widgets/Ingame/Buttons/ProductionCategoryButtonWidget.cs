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

namespace OpenRA.Mods.OpenKrush.Widgets.Ingame.Buttons;

using Common.Traits;
using Common.Widgets;

public class ProductionCategoryButtonWidget : SidebarButtonWidget
{
	public readonly string[] Categories;
	private readonly Hotkey hotkey;

	public ProductionCategoryButtonWidget(SidebarWidget sidebar, int index, string[] categories, string label)
		: base(sidebar, "unit")
	{
		this.Categories = categories;
		this.Bounds = new(0, index * SidebarButtonWidget.Size, SidebarButtonWidget.Size, SidebarButtonWidget.Size);
		this.TooltipTitle = label;
		this.hotkey = Game.ModData.Hotkeys[$"Production{label}"].GetValue();
	}

	public override bool HandleKeyPress(KeyInput e)
	{
		if (!this.IsUsable() || e.Key != this.hotkey.Key || e.IsRepeat || e.Event != KeyInputEvent.Down || e.Modifiers != this.hotkey.Modifiers)
			return false;

		if (!this.Active)
		{
			this.Active = true;
			this.Sidebar.CloseAllBut(this);
		}
		else
		{
			for (var i = 0; i < this.Children.Count; i++)
			{
				var child = (ProductionPaletteColumnWidget)this.Children[i];

				if (!child.IsFocused)
					continue;

				child.IsFocused = false;
				((ProductionPaletteColumnWidget)this.Children[(i + 1) % this.Children.Count]).IsFocused = true;

				break;
			}
		}

		return true;
	}

	protected override bool HandleLeftClick(MouseInput mi)
	{
		if (!base.HandleLeftClick(mi))
			return false;

		if (this.Active)
			this.Sidebar.CloseAllBut(this);

		return true;
	}

	protected override bool IsUsable()
	{
		return this.Children.Count > 0;
	}

	public override void Tick()
	{
		var actors = this.Sidebar.IngameUi.World.ActorsHavingTrait<ProductionQueue>().Where(a => a.Owner == this.Sidebar.IngameUi.World.LocalPlayer);

		var queues = actors.SelectMany(actor => actor.TraitsImplementing<ProductionQueue>(), (actor, productionQueue) => new { actor, productionQueue })
			.Where(t => this.Categories.Contains(t.productionQueue.Info.Type))
			.Where(t => t.productionQueue.BuildableItems().Any())
			.Select(t => t.productionQueue)
			.ToList();

		var children = this.Children.ToArray();

		foreach (var child in children)
		{
			if (!queues.Contains(((ProductionPaletteColumnWidget)child).Queue))
				this.RemoveChild(child);
			else
				queues.Remove(((ProductionPaletteColumnWidget)child).Queue);
		}

		foreach (var queue in queues)
			this.AddChild(new ProductionPaletteColumnWidget(this.Sidebar, queue));

		this.Children.Sort(
			(a, b) =>
			{
				var typeA = ((ProductionPaletteColumnWidget)a).Queue.Info.Type;
				var typeB = ((ProductionPaletteColumnWidget)b).Queue.Info.Type;

				if (typeA != typeB)
					return string.Compare(typeA, typeB, StringComparison.Ordinal);

				var idA = ((ProductionPaletteColumnWidget)a).Queue.Actor.ActorID;
				var idB = ((ProductionPaletteColumnWidget)b).Queue.Actor.ActorID;

				return (int)idA - (int)idB;
			}
		);

		var focused = -1;

		for (var i = 0; i < this.Children.Count; i++)
		{
			var palette = (ProductionPaletteColumnWidget)this.Children[i];
			palette.Bounds.X = (i + 1) * SidebarButtonWidget.Size * -1;

			if (palette.IsFocused)
				focused = i;
		}

		if (this.Children.Count == 0)
			this.Active = false;
		else if (focused == -1)
			((ProductionPaletteColumnWidget)this.Children[0]).IsFocused = true;

		this.Children.ForEach(c => c.Visible = this.Active);
		this.Type = this.Children.Count > 0 ? "unit" : "button";
	}

	protected override void DrawContents()
	{
		this.Sidebar.Buttons.PlayFetchIndex(this.Categories[0], () => 0);
		WidgetUtils.DrawSpriteCentered(this.Sidebar.Buttons.Image, this.Sidebar.IngameUi.Palette, this.Center + new int2(0, this.Active ? 1 : 0));
	}

	public void SelectFactory(Actor factory)
	{
		var productionQueue = factory.TraitsImplementing<ProductionQueue>().FirstOrDefault(pq => this.Categories.Contains(pq.Info.Type));
		var productionPalettes = this.Children.OfType<ProductionPaletteColumnWidget>().ToArray();

		foreach (var productionPalette in productionPalettes)
			productionPalette.IsFocused = productionPalette.Queue == productionQueue;
	}
}

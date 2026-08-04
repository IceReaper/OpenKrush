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

using Buttons;
using Common.Traits;
using Common.Traits.Render;
using Common.Widgets;
using Mechanics.Construction.Orders;
using Mechanics.Construction.Traits;
using OpenRA.Widgets;
using Primitives;

// TODO implement support for the colored bar with "jump-to-factory" (only if producer is not the player!)
public class ProductionPaletteColumnWidget : Widget
{
	private readonly SidebarWidget sidebar;
	public readonly ProductionQueue Queue;
	private ActorInfo[] buildableItems = Array.Empty<ActorInfo>();
	private int scrollOffset;
	private int visibleIcons;

	public bool IsFocused;

	public ProductionPaletteColumnWidget(SidebarWidget sidebar, ProductionQueue queue)
	{
		this.sidebar = sidebar;
		this.Queue = queue;
		this.Bounds = new(0, 0, SidebarButtonWidget.Size, SidebarButtonWidget.Size);
	}

	public override bool HandleKeyPress(KeyInput e)
	{
		if (!this.IsFocused || e.IsRepeat || e.Event != KeyInputEvent.Down)
			return false;

		for (var i = 0; i < 15; i++)
		{
			if (e.Key != Game.ModData.Hotkeys[$"Production{i + 1:00}"].GetValue().Key)
				continue;

			if (i < this.Children.Count)
				((ProductionItemButtonWidget)this.Children[i]).ClickedLeft?.Invoke(
					new(MouseInputEvent.Down, MouseButton.None, int2.Zero, int2.Zero, e.Modifiers, 0)
				);

			return true;
		}

		return false;
	}

	public override bool HandleMouseInput(MouseInput mi)
	{
		return this.EventBounds.Contains(mi.Location);
	}

	public override void Tick()
	{
		this.buildableItems = this.Queue.BuildableItems().ToArray();

		this.visibleIcons = Math.Min(15, this.buildableItems.Length);
		this.Bounds.Width = SidebarWidget.GridWidth;
		this.Bounds.Height = SidebarWidget.GridHeight;
		this.scrollOffset = 0;

		var oldButtons = this.Children
			.Where(c => c is ProductionItemButtonWidget && this.buildableItems.All(b => b.Name != ((ProductionItemButtonWidget)c).Item))
			.ToArray();

		foreach (var oldButton in oldButtons)
			this.Children.Remove(oldButton);

		for (var i = 0; i < this.buildableItems.Length; i++)
		{
			var buildableItem = this.buildableItems[i];

			var button = this.Children.FirstOrDefault(c => c is ProductionItemButtonWidget widget && widget.Item == buildableItem.Name);

			if (button == null)
			{
				bool IsActive()
				{
					if (this.Queue is not SelfConstructingProductionQueue)
						return this.Queue.AllQueued().Any(item => item.Item.Equals(buildableItem.Name));

					return this.sidebar.IngameUi.World.OrderGenerator is PlaceSpecificBuildingOrderGenerator pbog && pbog.Name == buildableItem.Name;
				}

				var valued = buildableItem.TraitInfoOrDefault<ValuedInfo>();

				var buildTime = WidgetUtils.FormatTime(
					buildableItem.TraitInfoOrDefault<BuildableInfo>().BuildDuration,
					false,
					this.sidebar.IngameUi.World.Timestep
				);

				var description = buildableItem.TraitInfoOrDefault<TooltipDescriptionInfo>();
				var icon = buildableItem.TraitInfoOrDefault<RenderSpritesInfo>();

				button = new ProductionItemButtonWidget(this.sidebar)
				{
					Item = buildableItem.Name,
					Icon = icon.Image ?? buildableItem.Name,
					Progress = () =>
					{
						if (this.Queue is SelfConstructingProductionQueue)
							return -1;

						var queued = this.Queue.AllQueued().FirstOrDefault(q => q.Item == buildableItem.Name);

						return queued == null ? -1 : (queued.TotalTime - queued.RemainingTime) * 100 / queued.TotalTime;
					},
					Amount = () =>
					{
						var queued = this.Queue.AllQueued().Where(q => q.Item == buildableItem.Name).ToArray();

						return queued.FirstOrDefault()?.Infinite ?? false ? -1 : queued.Length;
					},
					ClickedLeft = mi =>
					{
						var count = mi.Modifiers.HasModifier(Modifiers.Shift) ? 5 :
							mi.Modifiers.HasModifier(Modifiers.Ctrl) ? 11 : 1;

						var actor = this.sidebar.IngameUi.World.Map.Rules.Actors[buildableItem.Name];

						if (actor.HasTraitInfo<BuildingInfo>())
						{
							if (IsActive())
								this.sidebar.IngameUi.World.CancelInputMode();
							else
							{
								this.sidebar.IngameUi.World.OrderGenerator = new PlaceSpecificBuildingOrderGenerator(
									this.Queue,
									buildableItem.Name,
									this.sidebar.IngameUi.WorldRenderer
								);
							}
						}
						else
							this.sidebar.IngameUi.World.IssueOrder(Order.StartProduction(this.Queue.Actor, buildableItem.Name, count));
					},
					ClickedRight = mi =>
					{
						var count = mi.Modifiers.HasModifier(Modifiers.Shift) ? 5 :
							mi.Modifiers.HasModifier(Modifiers.Ctrl) ? 11 : 1;

						this.sidebar.IngameUi.World.IssueOrder(Order.CancelProduction(this.Queue.Actor, buildableItem.Name, count));
					},
					IsActive = IsActive,
					IsFocused = () => this.Parent.Children.Count > 1 && this.IsFocused,
					TooltipTitle = buildableItem.TraitInfoOrDefault<TooltipInfo>().Name,
					TooltipText =
						$"{(valued == null ? "" : $"Cost: {valued.Cost}\n")}Time: {buildTime}{(description != null ? $"\n{description.Description}" : null)}"
				};

				this.AddChild(button);
			}

			if (i < this.scrollOffset || i >= this.scrollOffset + this.visibleIcons)
				button.Visible = false;
			else
			{
				button.Visible = true;
				button.Bounds.X = ((i - this.scrollOffset) % SidebarWidget.GridColumns) * SidebarButtonWidget.Size;
				button.Bounds.Y = ((i - this.scrollOffset) / SidebarWidget.GridColumns) * SidebarButtonWidget.Size;
			}
		}
	}
}

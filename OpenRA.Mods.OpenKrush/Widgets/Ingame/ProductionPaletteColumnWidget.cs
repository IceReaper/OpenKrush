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

		var lastItem = Math.Min(12, this.Children.Count);

		for (var i = 0; i < lastItem; i++)
		{
			if (e.Key != Game.ModData.Hotkeys[$"Production{i + 1:00}"].GetValue().Key)
				continue;

			((ProductionItemButtonWidget)this.Children[i]).ClickedLeft?.Invoke(
				new(MouseInputEvent.Down, MouseButton.None, int2.Zero, int2.Zero, e.Modifiers, 0)
			);

			return true;
		}

		return false;
	}

	public override bool HandleMouseInput(MouseInput mi)
	{
		// TODO this whole block can be removed when arrows are widgets!
		if (!this.EventBounds.Contains(mi.Location))
			return false;

		switch (mi.Event)
		{
			case MouseInputEvent.Down when mi.Location.Y - this.EventBounds.Y < this.visibleIcons * SidebarButtonWidget.Size:
				return true;

			case MouseInputEvent.Down:
			{
				var arrow = (mi.Location.X - this.EventBounds.X) / (SidebarButtonWidget.Size / 2);

				switch (arrow)
				{
					case 0 when this.scrollOffset > 0:
						Game.Sound.PlayNotification(this.sidebar.IngameUi.World.Map.Rules, null, "Sounds", "ClickSound", null);
						this.scrollOffset--;

						break;

					case 1 when this.scrollOffset + this.visibleIcons < this.buildableItems.Length:
						Game.Sound.PlayNotification(this.sidebar.IngameUi.World.Map.Rules, null, "Sounds", "ClickSound", null);
						this.scrollOffset++;

						break;
				}

				break;
			}

			case MouseInputEvent.Scroll:
				this.scrollOffset = Math.Max(0, Math.Min(this.scrollOffset += mi.Delta.Y < 0 ? 1 : -1, this.buildableItems.Length - this.visibleIcons));

				break;

			case MouseInputEvent.Move:
				break;

			case MouseInputEvent.Up:
				break;

			default:
				throw new ArgumentOutOfRangeException(Enum.GetName(mi.Event));
		}

		return true;
	}

	public override void Tick()
	{
		this.buildableItems = this.Queue.BuildableItems().ToArray();

		this.visibleIcons = Math.Min((Game.Renderer.Resolution.Height - this.RenderBounds.Top) / SidebarButtonWidget.Size, this.buildableItems.Length);
		this.Bounds.Height = this.visibleIcons * SidebarButtonWidget.Size;

		if (this.visibleIcons < this.buildableItems.Length)
		{
			this.visibleIcons = (Game.Renderer.Resolution.Height - this.RenderBounds.Top - SidebarButtonWidget.Size / 2) / SidebarButtonWidget.Size;
			this.Bounds.Height = this.visibleIcons * SidebarButtonWidget.Size + SidebarButtonWidget.Size / 2;
		}

		this.scrollOffset = Math.Max(0, Math.Min(this.scrollOffset, this.buildableItems.Length - this.visibleIcons));

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
				button.Bounds.Y = (i - this.scrollOffset) * SidebarButtonWidget.Size;
			}
		}
	}

	public override void Draw()
	{
		// TODO this whole block can be removed when arrows are widgets!
		if (this.visibleIcons >= this.buildableItems.Length)
			return;

		var position = new int2(
			this.RenderBounds.X + SidebarButtonWidget.Size / 4,
			this.RenderBounds.Y + SidebarButtonWidget.Size / 4 + this.visibleIcons * SidebarButtonWidget.Size
		);

		this.sidebar.Buttons.PlayFetchIndex("button-small", () => 0);
		WidgetUtils.DrawSpriteCentered(this.sidebar.Buttons.Image, this.sidebar.IngameUi.Palette, position);
		this.sidebar.Buttons.PlayFetchIndex("button-small-down", () => 0);
		WidgetUtils.DrawSpriteCentered(this.sidebar.Buttons.Image, this.sidebar.IngameUi.Palette, position);

		if (this.scrollOffset == 0)
		{
			WidgetUtils.FillRectWithColor(
				new(
					this.RenderBounds.X,
					this.RenderBounds.Y + this.visibleIcons * SidebarButtonWidget.Size,
					SidebarButtonWidget.Size / 2,
					SidebarButtonWidget.Size / 2
				),
				Color.FromArgb(128, 0, 0, 0)
			);
		}

		this.sidebar.Buttons.PlayFetchIndex("button-small", () => 0);
		WidgetUtils.DrawSpriteCentered(this.sidebar.Buttons.Image, this.sidebar.IngameUi.Palette, position + new int2(SidebarButtonWidget.Size / 2, 0));
		this.sidebar.Buttons.PlayFetchIndex("button-small-up", () => 0);
		WidgetUtils.DrawSpriteCentered(this.sidebar.Buttons.Image, this.sidebar.IngameUi.Palette, position + new int2(SidebarButtonWidget.Size / 2, 0));

		if (this.scrollOffset + this.visibleIcons == this.buildableItems.Length)
		{
			WidgetUtils.FillRectWithColor(
				new(
					this.RenderBounds.X + SidebarButtonWidget.Size / 2,
					this.RenderBounds.Y + this.visibleIcons * SidebarButtonWidget.Size,
					SidebarButtonWidget.Size / 2,
					SidebarButtonWidget.Size / 2
				),
				Color.FromArgb(128, 0, 0, 0)
			);
		}
	}
}

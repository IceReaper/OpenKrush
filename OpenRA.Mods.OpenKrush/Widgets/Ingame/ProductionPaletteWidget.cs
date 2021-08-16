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
	using System;
	using System.Linq;
	using Buttons;
	using Common.Traits;
	using Common.Traits.Render;
	using Common.Widgets;
	using Mechanics.Construction.Traits;
	using OpenRA.Widgets;
	using Orders;
	using Primitives;
	using ButtonWidget = Buttons.ButtonWidget;

	// TODO implement support for the colored bar with "jump-to-factory" (only if producer is not the player!)
	public class ProductionPaletteWidget : Widget
	{
		private readonly SidebarWidget sidebar;
		public readonly ProductionQueue Queue;
		private ActorInfo[] buildableItems;
		private int scrollOffset;
		private int visibleIcons;

		public bool IsFocused;

		public ProductionPaletteWidget(SidebarWidget sidebar, ProductionQueue queue)
		{
			this.sidebar = sidebar;
			Queue = queue;
			Bounds = new Rectangle(0, 0, ButtonWidget.Size, ButtonWidget.Size);
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (!IsFocused || e.IsRepeat || e.Event != KeyInputEvent.Down)
				return false;

			var lastItem = Math.Min(12, Children.Count);

			for (var i = 0; i < lastItem; i++)
			{
				if (e.Key != Game.ModData.Hotkeys[$"Production{i + 1:00}"].GetValue().Key)
					continue;

				((ProductionItemButtonWidget)Children[i]).ClickedLeft(
					new MouseInput(MouseInputEvent.Down, MouseButton.None, int2.Zero, int2.Zero, e.Modifiers, 0));

				return true;
			}

			return false;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			// TODO this whole block can be removed when arrows are widgets!
			if (!EventBounds.Contains(mi.Location))
				return false;

			if (mi.Event == MouseInputEvent.Down)
			{
				if (mi.Location.Y - EventBounds.Y < visibleIcons * ButtonWidget.Size)
					return true;

				var arrow = (mi.Location.X - EventBounds.X) / (ButtonWidget.Size / 2);

				if (arrow == 0 && scrollOffset + visibleIcons < buildableItems.Length)
				{
					Game.Sound.PlayNotification(sidebar.IngameUi.World.Map.Rules, null, "Sounds", "ClickSound", null);
					scrollOffset++;
				}
				else if (arrow == 1 && scrollOffset > 0)
				{
					Game.Sound.PlayNotification(sidebar.IngameUi.World.Map.Rules, null, "Sounds", "ClickSound", null);
					scrollOffset--;
				}
			}
			else if (mi.Event == MouseInputEvent.Scroll)
				scrollOffset = Math.Max(0, Math.Min(scrollOffset += mi.Delta.Y < 0 ? 1 : -1, buildableItems.Length - visibleIcons));

			return true;
		}

		public override void Tick()
		{
			buildableItems = Queue.BuildableItems().ToArray();

			visibleIcons = Math.Min((Game.Renderer.Resolution.Height - RenderBounds.Top) / ButtonWidget.Size, buildableItems.Length);
			Bounds.Height = visibleIcons * ButtonWidget.Size;

			if (visibleIcons < buildableItems.Length)
			{
				visibleIcons = (Game.Renderer.Resolution.Height - RenderBounds.Top - ButtonWidget.Size / 2) / ButtonWidget.Size;
				Bounds.Height = visibleIcons * ButtonWidget.Size + ButtonWidget.Size / 2;
			}

			scrollOffset = Math.Max(0, Math.Min(scrollOffset, buildableItems.Length - visibleIcons));

			var oldButtons = Children.Where(c => c is ProductionItemButtonWidget && buildableItems.All(b => b.Name != ((ProductionItemButtonWidget)c).Item))
				.ToArray();

			foreach (var oldButton in oldButtons)
				Children.Remove(oldButton);

			for (var i = 0; i < buildableItems.Length; i++)
			{
				var buildableItem = buildableItems[i];

				var button = Children.FirstOrDefault(
					c => c is ProductionItemButtonWidget widget && widget.Item == buildableItem.Name);

				if (button == null)
				{
					bool IsActive()
					{
						if (!(Queue is SelfConstructingProductionQueue))
							return Queue.AllQueued().Any(item => item.Item.Equals(buildableItem.Name));

						return sidebar.IngameUi.World.OrderGenerator is PlaceSpecificBuildingOrderGenerator pbog && pbog.Name == buildableItem.Name;
					}

					var valued = buildableItem.TraitInfoOrDefault<ValuedInfo>();
					var buildTime = WidgetUtils.FormatTime(buildableItem.TraitInfo<BuildableInfo>().BuildDuration, false, sidebar.IngameUi.World.Timestep);
					var description = buildableItem.TraitInfoOrDefault<TooltipDescriptionInfo>();
					var icon = buildableItem.TraitInfo<RenderSpritesInfo>();

					button = new ProductionItemButtonWidget(sidebar)
					{
						Item = buildableItem.Name,
						Icon = icon.Image ?? buildableItem.Name,
						Progress = () =>
						{
							if (Queue is SelfConstructingProductionQueue)
								return -1;

							var queued = Queue.AllQueued().FirstOrDefault(q => q.Item == buildableItem.Name);

							return queued == null ? -1 : (queued.TotalTime - queued.RemainingTime) * 100 / queued.TotalTime;
						},
						Amount = () =>
						{
							var queued = Queue.AllQueued().Where(q => q.Item == buildableItem.Name).ToArray();

							return queued.Length > 0 && queued.First().Infinite ? -1 : queued.Length;
						},
						ClickedLeft = mi =>
						{
							var count = mi.Modifiers.HasModifier(Modifiers.Shift) ? 5 : mi.Modifiers.HasModifier(Modifiers.Ctrl) ? 11 : 1;
							var actor = sidebar.IngameUi.World.Map.Rules.Actors[buildableItem.Name];

							if (actor.HasTraitInfo<BuildingInfo>())
							{
								if (IsActive())
									sidebar.IngameUi.World.CancelInputMode();
								else
									sidebar.IngameUi.World.OrderGenerator = new PlaceSpecificBuildingOrderGenerator(
										Queue,
										buildableItem.Name,
										sidebar.IngameUi.WorldRenderer);
							}
							else
								sidebar.IngameUi.World.IssueOrder(Order.StartProduction(Queue.Actor, buildableItem.Name, count));
						},
						ClickedRight = mi =>
						{
							var count = mi.Modifiers.HasModifier(Modifiers.Shift) ? 5 : mi.Modifiers.HasModifier(Modifiers.Ctrl) ? 11 : 1;
							sidebar.IngameUi.World.IssueOrder(Order.CancelProduction(Queue.Actor, buildableItem.Name, count));
						},
						IsActive = IsActive,
						IsFocused = () => Parent.Children.Count > 1 && IsFocused,
						TooltipTitle = buildableItem.TraitInfo<TooltipInfo>().Name,
						TooltipText =
							$"{(valued == null ? "" : $"Cost: {valued.Cost}\n")}Time: {buildTime}{(description != null ? $"\n{description.Description}" : null)}"
					};

					AddChild(button);
				}

				if (i < scrollOffset || i >= scrollOffset + visibleIcons)
					button.Visible = false;
				else
				{
					button.Visible = true;
					button.Bounds.Y = (i - scrollOffset) * ButtonWidget.Size;
				}
			}
		}

		public override void Draw()
		{
			// TODO this whole block can be removed when arrows are widgets!
			if (visibleIcons >= buildableItems.Length)
				return;

			var position = new int2(RenderBounds.X + ButtonWidget.Size / 4, RenderBounds.Y + ButtonWidget.Size / 4 + visibleIcons * ButtonWidget.Size);

			sidebar.Buttons.PlayFetchIndex("button-small", () => 0);
			WidgetUtils.DrawSpriteCentered(sidebar.Buttons.Image, sidebar.IngameUi.Palette, position);
			sidebar.Buttons.PlayFetchIndex("button-small-down", () => 0);
			WidgetUtils.DrawSpriteCentered(sidebar.Buttons.Image, sidebar.IngameUi.Palette, position);

			if (scrollOffset + visibleIcons == buildableItems.Length)
				WidgetUtils.FillRectWithColor(
					new Rectangle(RenderBounds.X, RenderBounds.Y + visibleIcons * ButtonWidget.Size, ButtonWidget.Size / 2, ButtonWidget.Size / 2),
					Color.FromArgb(128, 0, 0, 0));

			sidebar.Buttons.PlayFetchIndex("button-small", () => 0);
			WidgetUtils.DrawSpriteCentered(sidebar.Buttons.Image, sidebar.IngameUi.Palette, position + new int2(ButtonWidget.Size / 2, 0));
			sidebar.Buttons.PlayFetchIndex("button-small-up", () => 0);
			WidgetUtils.DrawSpriteCentered(sidebar.Buttons.Image, sidebar.IngameUi.Palette, position + new int2(ButtonWidget.Size / 2, 0));

			if (scrollOffset == 0)
				WidgetUtils.FillRectWithColor(
					new Rectangle(
						RenderBounds.X + ButtonWidget.Size / 2,
						RenderBounds.Y + visibleIcons * ButtonWidget.Size,
						ButtonWidget.Size / 2,
						ButtonWidget.Size / 2),
					Color.FromArgb(128, 0, 0, 0));
		}
	}
}

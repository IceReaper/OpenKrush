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
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Common.Traits;
	using Common.Widgets;
	using Primitives;
	using ProductionPaletteWidget = Ingame.ProductionPaletteWidget;

	public class ProductionCategoryButtonWidget : ButtonWidget
	{
		public readonly string[] Categories;
		private readonly Hotkey hotkey;

		public ProductionCategoryButtonWidget(SidebarWidget sidebar, int index, string[] categories, string label)
			: base(sidebar, "unit")
		{
			Categories = categories;
			Bounds = new Rectangle(0, index * ButtonWidget.Size, ButtonWidget.Size, ButtonWidget.Size);
			TooltipTitle = label;
			hotkey = Game.ModData.Hotkeys[$"Production{label}"].GetValue();
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (!IsUsable() || e.Key != hotkey.Key || e.IsRepeat || e.Event != KeyInputEvent.Down || e.Modifiers != hotkey.Modifiers)
				return false;

			if (!Active)
			{
				Active = true;
				sidebar.CloseAllBut(this);
			}
			else
			{
				for (var i = 0; i < Children.Count; i++)
				{
					var child = (ProductionPaletteWidget)Children[i];

					if (child.IsFocused)
					{
						child.IsFocused = false;
						((ProductionPaletteWidget)Children[(i + 1) % Children.Count]).IsFocused = true;

						break;
					}
				}
			}

			return true;
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			if (!base.HandleLeftClick(mi))
				return false;

			if (Active)
				sidebar.CloseAllBut(this);

			return true;
		}

		protected override bool IsUsable()
		{
			return Children.Count > 0;
		}

		public override void Tick()
		{
			var actors = sidebar.IngameUi.World.ActorsHavingTrait<ProductionQueue>().Where(a => a.Owner == sidebar.IngameUi.World.LocalPlayer);

			var queues = actors.SelectMany(actor => actor.TraitsImplementing<ProductionQueue>(), (actor, productionQueue) => new { actor, productionQueue })
				.Where(t => Categories.Contains(t.productionQueue.Info.Type))
				.Where(t => t.productionQueue.BuildableItems().Any())
				.Select(t => t.productionQueue).ToList();

			var children = Children.ToArray();

			foreach (var child in children)
			{
				if (!queues.Contains(((ProductionPaletteWidget)child).Queue))
					RemoveChild(child);
				else
					queues.Remove(((ProductionPaletteWidget)child).Queue);
			}

			foreach (var queue in queues)
				AddChild(new ProductionPaletteWidget(sidebar, queue));

			Children.Sort(
				(a, b) =>
				{
					var typeA = ((ProductionPaletteWidget)a).Queue.Info.Type;
					var typeB = ((ProductionPaletteWidget)b).Queue.Info.Type;

					if (typeA != typeB)
						return string.Compare(typeA, typeB, StringComparison.Ordinal);

					var idA = ((ProductionPaletteWidget)a).Queue.Actor.ActorID;
					var idB = ((ProductionPaletteWidget)b).Queue.Actor.ActorID;

					return (int)idA - (int)idB;
				});

			var focused = -1;

			for (var i = 0; i < Children.Count; i++)
			{
				var palette = (ProductionPaletteWidget)Children[i];
				palette.Bounds.X = (i + 1) * ButtonWidget.Size * -1;

				if (palette.IsFocused)
					focused = i;
			}

			if (Children.Count == 0)
				Active = false;
			else if (focused == -1)
				((ProductionPaletteWidget)Children[0]).IsFocused = true;

			Children.ForEach(c => c.Visible = Active);
			type = Children.Count > 0 ? "unit" : "button";
		}

		protected override void DrawContents()
		{
			sidebar.Buttons.PlayFetchIndex(Categories[0], () => 0);
			WidgetUtils.DrawSpriteCentered(sidebar.Buttons.Image, sidebar.IngameUi.Palette, center + new int2(0, Active ? 1 : 0));
		}

		public void SelectFactory(Actor factory)
		{
			var productionQueue = factory.TraitsImplementing<ProductionQueue>().First(pq => Categories.Contains(pq.Info.Type));
			var productionPalettes = Children.OfType<ProductionPaletteWidget>().ToArray();

			foreach (var productionPalette in productionPalettes)
				productionPalette.IsFocused = productionPalette.Queue == productionQueue;
		}
	}
}

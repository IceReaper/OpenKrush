using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets.Ingame.Buttons
{
	public class ProductionCategoryButtonWidget : ButtonWidget
	{
		public readonly string[] Categories;
		private Hotkey hotkey;

		public ProductionCategoryButtonWidget(SidebarWidget sidebar, int index, string[] categories, string label) : base(sidebar, "unit")
		{
			Categories = categories;
			Bounds = new Rectangle(0, index * Size, Size, Size);
			TooltipTitle = label;
			hotkey = Game.ModData.Hotkeys["Production" + label].GetValue();
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
					var child = (ProductionPaletteWidget) Children[i];
					if (child.IsFocused)
					{
						child.IsFocused = false;
						((ProductionPaletteWidget) Children[(i + 1) % Children.Count]).IsFocused = true;
						break;
					}
				}
			}

			return true;
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			if (base.HandleLeftClick(mi))
			{
				if (Active)
					sidebar.CloseAllBut(this);

				return true;
			}

			return false;
		}

		protected override bool IsUsable()
		{
			return Children.Count > 0;
		}

		public override void Tick()
		{
			var queues = new List<ProductionQueue>();
			var actors = sidebar.IngameUi.World.ActorsHavingTrait<ProductionQueue>().Where(a => a.Owner == sidebar.IngameUi.World.LocalPlayer);

			foreach (var actor in actors)
			{
				var productionQueues = actor.TraitsImplementing<ProductionQueue>();

				foreach (var productionQueue in productionQueues)
				{
					if (!Categories.Contains(productionQueue.Info.Type))
						continue;

					if (!productionQueue.BuildableItems().Any())
						continue;

					queues.Add(productionQueue);
				}
			}

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

			Children.Sort((a, b) =>
			{
				var typeA = ((ProductionPaletteWidget) a).Queue.Info.Type;
				var typeB = ((ProductionPaletteWidget) b).Queue.Info.Type;

				if (typeA == typeB)
				{
					var idA = ((ProductionPaletteWidget) a).Queue.Actor.ActorID;
					var idB = ((ProductionPaletteWidget) b).Queue.Actor.ActorID;

					return (int)idA - (int)idB;
				}

				return string.Compare(typeA, typeB, StringComparison.Ordinal);
			});

			var focused = -1;
			
			for (var i = 0; i < Children.Count; i++)
			{
				var palette = (ProductionPaletteWidget)Children[i];
				palette.Bounds.X = (i + 1) * Size * -1;
				
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
			WidgetUtils.DrawSHPCentered(sidebar.Buttons.Image, center + new int2(0, Active ? 1 : 0), sidebar.IngameUi.Palette);
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

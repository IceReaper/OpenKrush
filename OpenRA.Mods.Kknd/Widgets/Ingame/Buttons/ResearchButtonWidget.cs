#region Copyright & License Information
/*
 * Copyright 2016-2018 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Mods.Kknd.Orders;
using OpenRA.Mods.Kknd.Traits.Research;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets.Ingame.Buttons
{
	public class ResearchButtonWidget : ButtonWidget
	{
		private bool researchAvailable;
		private bool autoResearchEnabled;

		public ResearchButtonWidget(SidebarWidget sidebar) : base(sidebar, "button")
		{
			TooltipTitle = "Research";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (IsUsable() && !e.IsRepeat && e.Event == KeyInputEvent.Down
			    && e.Key == Game.ModData.Hotkeys["Research"].GetValue().Key && e.Modifiers == Game.ModData.Hotkeys["Research"].GetValue().Modifiers)
			{
				Active = !Active;

				if (Active)
					sidebar.IngameUi.World.OrderGenerator = new ResearchOrderGenerator();
				else if (sidebar.IngameUi.World.OrderGenerator != null && sidebar.IngameUi.World.OrderGenerator is ResearchOrderGenerator)
					sidebar.IngameUi.World.CancelInputMode();

				return true;
			}

			return false;
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			if (base.HandleLeftClick(mi))
			{
				if (autoResearchEnabled)
					autoResearchEnabled = false;
				else if (Active)
					sidebar.IngameUi.World.OrderGenerator = new ResearchOrderGenerator();
				else if (sidebar.IngameUi.World.OrderGenerator != null && sidebar.IngameUi.World.OrderGenerator is ResearchOrderGenerator)
					sidebar.IngameUi.World.CancelInputMode();

				return true;
			}

			return false;
		}

		protected override bool HandleRightClick(MouseInput mi)
		{
			Game.Sound.PlayNotification(sidebar.IngameUi.World.Map.Rules, null, "Sounds", "ClickSound", null);
			autoResearchEnabled = !autoResearchEnabled;
			return true;
		}

		protected override bool IsUsable()
		{
			return researchAvailable;
		}

		public override void Tick()
		{
			var res = sidebar.IngameUi.World.ActorsWithTrait<Researches>().Where(e => e.Actor.Owner == sidebar.IngameUi.World.LocalPlayer && !e.Trait.IsTraitDisabled).ToArray();
			researchAvailable = res.Length > 0;
			autoResearchEnabled = researchAvailable && autoResearchEnabled;
			Active = !autoResearchEnabled && sidebar.IngameUi.World.OrderGenerator != null && sidebar.IngameUi.World.OrderGenerator is ResearchOrderGenerator;

			if (autoResearchEnabled && sidebar.IngameUi.World.WorldTick % 50 == 0)
			{
				var pair = res.FirstOrDefault(r => !r.Trait.IsResearching);

				if (pair.Trait == null)
					return;

				var researchables = sidebar.IngameUi.World.Actors.Where(a =>
				{
					if (a.Owner != sidebar.IngameUi.World.LocalPlayer)
						return false;

					var researchable = a.TraitOrDefault<Researchable>();

					return researchable != null && !researchable.IsTraitDisabled && researchable.Level < researchable.Info.MaxLevel && researchable.Researches == null;
				}).ToArray();

				if (researchables.Length == 0)
					return;

				var target = researchables[sidebar.IngameUi.World.LocalRandom.Next(0, researchables.Length)];
				sidebar.IngameUi.World.IssueOrder(new Order(ResearchOrderTargeter.Id, pair.Actor, Target.FromActor(target), false));
			}
		}

		protected override void DrawContents()
		{
			if (autoResearchEnabled)
				WidgetUtils.FillRectWithColor(RenderBounds, Color.FromArgb(25, 255, 255, 255));
			sidebar.Buttons.PlayFetchIndex("research", () => 0);
			WidgetUtils.DrawSHPCentered(sidebar.Buttons.Image, center + new int2(0, Active ? 1 : 0), sidebar.IngameUi.Palette);
		}
	}
}

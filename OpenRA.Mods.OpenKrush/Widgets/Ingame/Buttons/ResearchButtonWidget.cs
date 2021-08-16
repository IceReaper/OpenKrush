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
	using System.Linq;
	using Common.Widgets;
	using Mechanics.Researching;
	using Mechanics.Researching.Orders;
	using Mechanics.Researching.Traits;
	using OpenRA.Traits;
	using Primitives;

	public class ResearchButtonWidget : ButtonWidget
	{
		private bool researchAvailable;
		private bool autoResearchEnabled;

		public ResearchButtonWidget(SidebarWidget sidebar)
			: base(sidebar, "button")
		{
			TooltipTitle = "Research";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (!IsUsable()
				|| e.IsRepeat
				|| e.Event != KeyInputEvent.Down
				|| e.Key != Game.ModData.Hotkeys["Research"].GetValue().Key
				|| e.Modifiers != Game.ModData.Hotkeys["Research"].GetValue().Modifiers)
				return false;

			Active = !Active;

			if (Active)
				sidebar.IngameUi.World.OrderGenerator = new ResearchOrderGenerator();
			else if (sidebar.IngameUi.World.OrderGenerator is ResearchOrderGenerator)
				sidebar.IngameUi.World.CancelInputMode();

			return true;
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			if (!base.HandleLeftClick(mi))
				return false;

			if (autoResearchEnabled)
				autoResearchEnabled = false;
			else if (Active)
				sidebar.IngameUi.World.OrderGenerator = new ResearchOrderGenerator();
			else if (sidebar.IngameUi.World.OrderGenerator is ResearchOrderGenerator)
				sidebar.IngameUi.World.CancelInputMode();

			return true;
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
			var res = sidebar.IngameUi.World.ActorsWithTrait<Researches>()
				.Where(e => e.Actor.Owner == sidebar.IngameUi.World.LocalPlayer && !e.Trait.IsTraitDisabled)
				.ToArray();

			researchAvailable = res.Length > 0;
			autoResearchEnabled = researchAvailable && autoResearchEnabled;
			Active = !autoResearchEnabled && sidebar.IngameUi.World.OrderGenerator is ResearchOrderGenerator;

			if (!autoResearchEnabled || sidebar.IngameUi.World.WorldTick % 50 != 0)
				return;

			var pair = res.FirstOrDefault(r => r.Trait.GetState() != ResarchState.Researching);

			if (pair.Trait == null)
				return;

			var researchables = sidebar.IngameUi.World.Actors.Where(
					a =>
					{
						if (a.Owner != sidebar.IngameUi.World.LocalPlayer)
							return false;

						var researchable = a.TraitOrDefault<Researchable>();

						return researchable != null
							&& !researchable.IsTraitDisabled
							&& researchable.Level < researchable.MaxLevel
							&& researchable.ResearchedBy == null;
					})
				.ToArray();

			if (researchables.Length == 0)
				return;

			var target = researchables[sidebar.IngameUi.World.LocalRandom.Next(0, researchables.Length)];
			sidebar.IngameUi.World.IssueOrder(new Order(ResearchOrderTargeter.Id, pair.Actor, Target.FromActor(target), false));
		}

		protected override void DrawContents()
		{
			if (autoResearchEnabled)
				WidgetUtils.FillRectWithColor(RenderBounds, Color.FromArgb(25, 255, 255, 255));

			sidebar.Buttons.PlayFetchIndex("research", () => 0);
			WidgetUtils.DrawSpriteCentered(sidebar.Buttons.Image, sidebar.IngameUi.Palette, center + new int2(0, Active ? 1 : 0));
		}
	}
}

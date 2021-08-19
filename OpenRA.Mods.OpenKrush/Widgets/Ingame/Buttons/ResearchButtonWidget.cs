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
	using Common.Widgets;
	using Mechanics.Researching;
	using Mechanics.Researching.Orders;
	using Mechanics.Researching.Traits;
	using Primitives;
	using System.Linq;
	using Traits;

	public class ResearchButtonWidget : SidebarButtonWidget
	{
		private bool researchAvailable;
		private bool autoResearchEnabled;

		public ResearchButtonWidget(SidebarWidget sidebar)
			: base(sidebar, "button")
		{
			this.TooltipTitle = "Research";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (!this.IsUsable()
				|| e.IsRepeat
				|| e.Event != KeyInputEvent.Down
				|| e.Key != Game.ModData.Hotkeys["Research"].GetValue().Key
				|| e.Modifiers != Game.ModData.Hotkeys["Research"].GetValue().Modifiers)
				return false;

			this.Active = !this.Active;

			if (this.Active)
				this.Sidebar.IngameUi.World.OrderGenerator = new ResearchOrderGenerator();
			else if (this.Sidebar.IngameUi.World.OrderGenerator is ResearchOrderGenerator)
				this.Sidebar.IngameUi.World.CancelInputMode();

			return true;
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			if (!base.HandleLeftClick(mi))
				return false;

			if (this.autoResearchEnabled)
				this.autoResearchEnabled = false;
			else if (this.Active)
				this.Sidebar.IngameUi.World.OrderGenerator = new ResearchOrderGenerator();
			else if (this.Sidebar.IngameUi.World.OrderGenerator is ResearchOrderGenerator)
				this.Sidebar.IngameUi.World.CancelInputMode();

			return true;
		}

		protected override bool HandleRightClick(MouseInput mi)
		{
			Game.Sound.PlayNotification(this.Sidebar.IngameUi.World.Map.Rules, null, "Sounds", "ClickSound", null);
			this.autoResearchEnabled = !this.autoResearchEnabled;

			return true;
		}

		protected override bool IsUsable()
		{
			return this.researchAvailable;
		}

		public override void Tick()
		{
			var res = this.Sidebar.IngameUi.World.ActorsWithTrait<Researches>()
				.Where(e => e.Actor.Owner == this.Sidebar.IngameUi.World.LocalPlayer && !e.Trait.IsTraitDisabled)
				.ToArray();

			this.researchAvailable = res.Length > 0;
			this.autoResearchEnabled = this.researchAvailable && this.autoResearchEnabled;
			this.Active = !this.autoResearchEnabled && this.Sidebar.IngameUi.World.OrderGenerator is ResearchOrderGenerator;

			if (!this.autoResearchEnabled || this.Sidebar.IngameUi.World.WorldTick % 50 != 0)
				return;

			var pair = res.FirstOrDefault(r => r.Trait.GetState() != ResarchState.Researching);

			if (pair.Trait == null)
				return;

			var researchables = this.Sidebar.IngameUi.World.Actors.Where(
					a =>
					{
						if (a.Owner != this.Sidebar.IngameUi.World.LocalPlayer)
							return false;

						var researchable = a.TraitOrDefault<Researchable>();

						return researchable is { IsTraitDisabled: false } && researchable.Level < researchable.MaxLevel && researchable.ResearchedBy == null;
					}
				)
				.ToArray();

			if (researchables.Length == 0)
				return;

			var target = researchables[this.Sidebar.IngameUi.World.LocalRandom.Next(0, researchables.Length)];
			this.Sidebar.IngameUi.World.IssueOrder(new(ResearchOrderTargeter.Id, pair.Actor, Target.FromActor(target), false));
		}

		protected override void DrawContents()
		{
			if (this.autoResearchEnabled)
				WidgetUtils.FillRectWithColor(this.RenderBounds, Color.FromArgb(25, 255, 255, 255));

			this.Sidebar.Buttons.PlayFetchIndex("research", () => 0);
			WidgetUtils.DrawSpriteCentered(this.Sidebar.Buttons.Image, this.Sidebar.IngameUi.Palette, this.Center + new int2(0, this.Active ? 1 : 0));
		}
	}
}

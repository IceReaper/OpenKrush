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
	using System.Linq;
	using Common.Traits;
	using Common.Widgets;

	public sealed class BomberButtonWidget : ButtonWidget
	{
		public BomberButtonWidget(SidebarWidget sidebar)
			: base(sidebar, "button")
		{
			TooltipTitle = "Aircrafts";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (!IsUsable() || e.IsRepeat || e.Event != KeyInputEvent.Down)
				return false;

			if (!Active
				&& e.Key == Game.ModData.Hotkeys["Superweapons"].GetValue().Key
				&& e.Modifiers == Game.ModData.Hotkeys["Superweapons"].GetValue().Modifiers)
			{
				Active = true;
				sidebar.CloseAllBut(this);

				return true;
			}

			if (!Active)
				return false;

			var lastItem = Math.Min(12, Children.Count);

			for (var i = 0; i < lastItem; i++)
			{
				if (e.Key != Game.ModData.Hotkeys[$"Production{i + 1:00}"].GetValue().Key
					|| e.Modifiers != Game.ModData.Hotkeys[$"Production{i + 1}"].GetValue().Modifiers)
					continue;

				((ProductionItemButtonWidget)Children[i]).ClickedLeft(
					new MouseInput(MouseInputEvent.Down, MouseButton.None, int2.Zero, int2.Zero, e.Modifiers, 0));

				return true;
			}

			return false;
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
			var spm = sidebar.IngameUi.World.LocalPlayer.PlayerActor.Trait<SupportPowerManager>();
			var powers = spm.Powers.Values.Where(p => !p.Disabled).ToArray();

			var oldButtons = Children.Where(c => powers.All(b => b.Key != ((ProductionItemButtonWidget)c).Item)).ToArray();

			foreach (var oldButton in oldButtons)
				Children.Remove(oldButton);

			for (var i = 0; i < powers.Length; i++)
			{
				var power = powers[i];

				var button = Children.FirstOrDefault(
					c => c is ProductionItemButtonWidget widget && widget.Item == power.Key);

				if (button == null)
				{
					button = new ProductionItemButtonWidget(sidebar)
					{
						Item = power.Info.IconImage,
						Icon = power.Info.IconImage,
						Progress = () => power.Ready ? -1 : (power.TotalTicks - power.RemainingTicks) * 100 / power.TotalTicks,
						Amount = () => 0,
						ClickedLeft = mi => power.Target(),
						ClickedRight = null,
						IsActive = () => sidebar.IngameUi.World.OrderGenerator is SelectGenericPowerTarget og && og.OrderKey == power.Key,
						IsFocused = () => false,
						TooltipTitle = power.Info.Description,
						TooltipText = null
					};

					AddChild(button);
				}

				button.Visible = Active;
				button.Bounds.X = (-1 - i) * ButtonWidget.Size;
			}

			if (Children.Count == 0)
				Active = false;

			type = Children.Count > 0 ? "unit" : "button";
		}

		protected override void DrawContents()
		{
			sidebar.Buttons.PlayFetchIndex("aircraft", () => 0);
			WidgetUtils.DrawSpriteCentered(sidebar.Buttons.Image, sidebar.IngameUi.Palette, center + new int2(0, Active ? 1 : 0));
		}
	}
}

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

public sealed class BomberButtonWidget : SidebarButtonWidget
{
	public BomberButtonWidget(SidebarWidget sidebar)
		: base(sidebar, "button")
	{
		this.TooltipTitle = "Aircrafts";
	}

	public override bool HandleKeyPress(KeyInput e)
	{
		if (!this.IsUsable() || e.IsRepeat || e.Event != KeyInputEvent.Down)
			return false;

		switch (this.Active)
		{
			case false when e.Key == Game.ModData.Hotkeys["Superweapons"].GetValue().Key
				&& e.Modifiers == Game.ModData.Hotkeys["Superweapons"].GetValue().Modifiers:
				this.Active = true;
				this.Sidebar.CloseAllBut(this);

				return true;

			case false:
				return false;
		}

		var lastItem = Math.Min(12, this.Children.Count);

		for (var i = 0; i < lastItem; i++)
		{
			if (e.Key != Game.ModData.Hotkeys[$"Production{i + 1:00}"].GetValue().Key
				|| e.Modifiers != Game.ModData.Hotkeys[$"Production{i + 1}"].GetValue().Modifiers)
				continue;

			((ProductionItemButtonWidget)this.Children[i]).ClickedLeft?.Invoke(
				new(MouseInputEvent.Down, MouseButton.None, int2.Zero, int2.Zero, e.Modifiers, 0)
			);

			return true;
		}

		return false;
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
		var spm = this.Sidebar.IngameUi.World.LocalPlayer.PlayerActor.TraitOrDefault<SupportPowerManager>();
		var powers = spm.Powers.Values.Where(p => !p.Disabled).ToArray();

		var oldButtons = this.Children.Where(c => powers.All(b => b.Key != ((ProductionItemButtonWidget)c).Item)).ToArray();

		foreach (var oldButton in oldButtons)
			this.Children.Remove(oldButton);

		for (var i = 0; i < powers.Length; i++)
		{
			var power = powers[i];

			var button = this.Children.FirstOrDefault(c => c is ProductionItemButtonWidget widget && widget.Item == power.Key);

			if (button == null)
			{
				button = new ProductionItemButtonWidget(this.Sidebar)
				{
					Item = power.Info.IconImage,
					Icon = power.Info.IconImage,
					Progress = () => power.Ready ? -1 : (power.TotalTicks - power.RemainingTicks) * 100 / power.TotalTicks,
					Amount = () => 0,
					ClickedLeft = _ => power.Target(),
					ClickedRight = null,
					IsActive = () => this.Sidebar.IngameUi.World.OrderGenerator is SelectGenericPowerTarget og && og.OrderKey == power.Key,
					IsFocused = () => false,
					TooltipTitle = power.Info.Description,
					TooltipText = null
				};

				this.AddChild(button);
			}

			button.Visible = this.Active;
			button.Bounds.X = (-1 - i) * SidebarButtonWidget.Size;
		}

		if (this.Children.Count == 0)
			this.Active = false;

		this.Type = this.Children.Count > 0 ? "unit" : "button";
	}

	protected override void DrawContents()
	{
		this.Sidebar.Buttons.PlayFetchIndex("aircraft", () => 0);
		WidgetUtils.DrawSpriteCentered(this.Sidebar.Buttons.Image, this.Sidebar.IngameUi.Palette, this.Center + new int2(0, this.Active ? 1 : 0));
	}
}

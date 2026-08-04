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

using System;
using System.Linq;
using Buttons;
using Common.Orders;
using Common.Orders;
using Common.Traits;
using Common.Widgets;
using Graphics;
using Mechanics.Construction.Orders;
using Mechanics.Construction.Traits;
using Mechanics.Researching;
using Mechanics.Researching.Orders;
using Mechanics.Researching.Traits;
using Mechanics.Technicians.Orders;
using Mechanics.Technicians.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;
using Primitives;

// SC2-like command card shown in the bottom-right grid when units or buildings are selected.
public sealed class UnitCommandWidget : Widget
{
	private sealed class CommandSlot
	{
		public readonly string Label;
		public readonly string TooltipTitle;
		public readonly string TooltipText;
		public readonly Action OnClick;
		public readonly Func<bool> IsDisabled;
		public readonly Func<bool> IsHighlighted;
		public readonly string? Icon;

		public CommandSlot(string label, string tooltipTitle, string tooltipText, Action onClick, Func<bool> isDisabled, string? icon = null, Func<bool>? isHighlighted = null)
		{
			this.Label = label;
			this.TooltipTitle = tooltipTitle;
			this.TooltipText = tooltipText;
			this.OnClick = onClick;
			this.IsDisabled = isDisabled;
			this.IsHighlighted = isHighlighted ?? (() => false);
			this.Icon = icon;
		}
	}

	private readonly SidebarWidget sidebar;
	private readonly World world;
	private readonly SpriteFont? labelFont;
	private readonly SpriteFont? keyFont;
	private readonly CommandSlot[] slots = new CommandSlot[15];

	private Actor[] selectedActors = Array.Empty<Actor>();
	private TraitPair<IIssueDeployOrder>[] selectedDeploys = Array.Empty<TraitPair<IIssueDeployOrder>>();
	private TraitPair<AutoTarget>[] actorStances = Array.Empty<TraitPair<AutoTarget>>();
	private int selectionHash;

	private bool attackMoveDisabled = true;
	private bool guardDisabled = true;
	private bool scatterDisabled = true;
	private bool stopDisabled = true;

	private int deployHighlighted;
	private int scatterHighlighted;
	private int stopHighlighted;

	private bool hadSelection;
	private bool isBuildingSelection;
	private bool wasBuildingSelection;

	public UnitCommandWidget(SidebarWidget sidebar)
	{
		this.sidebar = sidebar;
		this.world = sidebar.IngameUi.World;

		Game.Renderer.Fonts.TryGetValue("Tiny", out this.labelFont);
		Game.Renderer.Fonts.TryGetValue("TinyBold", out this.keyFont);

		this.Bounds = new(SidebarWidget.BarWidth - SidebarWidget.GridWidth, 0, SidebarWidget.GridWidth, SidebarWidget.GridHeight);
	}

	private void PopulateUnitSlots()
	{
		Array.Clear(this.slots, 0, this.slots.Length);

		this.slots[0] = new(
			"Move",
			"Move",
			"Move to a location. Right-click on the map.",
			() => this.world.CancelInputMode(),
			() => false
		);

		this.slots[1] = new(
			"Attack",
			"Attack Move",
			"Move while attacking enemies on the way.",
			() => this.ToggleAttackMove(true),
			() => { this.UpdateStateIfNecessary(); return this.attackMoveDisabled; },
			isHighlighted: () => this.world.OrderGenerator is AttackMoveOrderGenerator
		);

		this.slots[2] = new(
			"Stop",
			"Stop",
			"Cancel all orders.",
			() =>
			{
				this.stopHighlighted = 2;
				this.PerformKeyboardOrderOnSelection(a => new Order("Stop", a, false));
			},
			() => { this.UpdateStateIfNecessary(); return this.stopDisabled; },
			isHighlighted: () => this.stopHighlighted > 0
		);

		this.slots[3] = new(
			"Guard",
			"Guard",
			"Guard a location or unit.",
			() => this.ToggleGuard(true),
			() => { this.UpdateStateIfNecessary(); return this.guardDisabled; },
			isHighlighted: () => this.world.OrderGenerator is GuardOrderGenerator
		);

		this.slots[4] = new(
			"Scatter",
			"Scatter",
			"Scatter the selected units.",
			() =>
			{
				this.scatterHighlighted = 2;
				this.PerformKeyboardOrderOnSelection(a => new Order("Scatter", a, false));
			},
			() => { this.UpdateStateIfNecessary(); return this.scatterDisabled; },
			isHighlighted: () => this.scatterHighlighted > 0
		);

		this.slots[5] = new(
			"Attack All",
			"Attack Anything",
			"Attack everything hostile in range.",
			() => this.SetSelectionStance(UnitStance.AttackAnything),
			() => { this.UpdateStateIfNecessary(); return this.actorStances.Length == 0; },
			isHighlighted: () => this.GetStanceHighlighted(UnitStance.AttackAnything)
		);

		this.slots[6] = new(
			"Defend",
			"Defend",
			"Attack enemies in range, but never hunt them.",
			() => this.SetSelectionStance(UnitStance.Defend),
			() => { this.UpdateStateIfNecessary(); return this.actorStances.Length == 0; },
			isHighlighted: () => this.GetStanceHighlighted(UnitStance.Defend)
		);

		this.slots[7] = new(
			"Return",
			"Return Fire",
			"Only return fire when attacked.",
			() => this.SetSelectionStance(UnitStance.ReturnFire),
			() => { this.UpdateStateIfNecessary(); return this.actorStances.Length == 0; },
			isHighlighted: () => this.GetStanceHighlighted(UnitStance.ReturnFire)
		);

		this.slots[8] = new(
			"Hold",
			"Hold Fire",
			"Never fire.",
			() => this.SetSelectionStance(UnitStance.HoldFire),
			() => { this.UpdateStateIfNecessary(); return this.actorStances.Length == 0; },
			isHighlighted: () => this.GetStanceHighlighted(UnitStance.HoldFire)
		);

		this.slots[9] = new(
			"Deploy",
			"Deploy",
			"Deploy or undeploy the selected units.",
			() =>
			{
				this.deployHighlighted = 2;
				this.PerformDeployOrderOnSelection(Game.GetModifierKeys().HasModifier(Modifiers.Shift));
			},
			() =>
			{
				this.UpdateStateIfNecessary();
				var queued = Game.GetModifierKeys().HasModifier(Modifiers.Shift);
				return !this.selectedDeploys.Any(pair => pair.Trait.CanIssueDeployOrder(pair.Actor, queued));
			},
			isHighlighted: () => this.deployHighlighted > 0
		);
	}

	private void PopulateBuildingSlots()
	{
		Array.Clear(this.slots, 0, this.slots.Length);

		this.slots[12] = new(
			"Sell",
			"Sell",
			"Sell the selected building.",
			() =>
			{
				if (this.selectedActors.Length > 0)
					this.world.IssueOrder(new("Sell", this.selectedActors[0], false));
			},
			() => !this.world.ActorsWithTrait<SelfConstructing>().Any(e => e.Actor.Owner == this.world.LocalPlayer),
			icon: "sell"
		);

		this.slots[13] = new(
			"Repair",
			"Repair",
			"Send a technician to repair the building.",
			() =>
			{
				if (this.selectedActors.Length == 0)
					return;

				var technician = this.world.ActorsWithTrait<Technician>()
					.FirstOrDefault(t => t.Actor.Owner == this.world.LocalPlayer && t.Actor.IsIdle);

				if (technician != null)
					this.world.IssueOrder(new(TechnicianEnterOrderTargeter.Id, technician.Actor, Target.FromActor(this.selectedActors[0]), true));
			},
			() => !this.world.ActorsWithTrait<Technician>().Any(e => e.Actor.Owner == this.world.LocalPlayer && e.Actor.IsIdle),
			icon: "repair"
		);

		this.slots[14] = new(
			"Research",
			"Research",
			"Research this building.",
			() =>
			{
				if (this.selectedActors.Length == 0)
					return;

				var researchActors = this.world.ActorsWithTrait<Researches>()
					.Where(a => a.Actor.Owner == this.world.LocalPlayer && !a.Trait.IsTraitDisabled)
					.ToArray();

				if (researchActors.Length == 0)
				{
					TooltipWidget.Instance?.ShowCentered("No research building", "Build a research lab first.");
					return;
				}

				var free = researchActors.FirstOrDefault(a => a.Trait.GetState() != ResarchState.Researching);
				if (free != null)
					this.world.IssueOrder(new(ResearchOrderTargeter.Id, free.Actor, Target.FromActor(this.selectedActors[0]), false));
				else
					TooltipWidget.Instance?.ShowCentered("Already researching", "Wait for the current research to finish.");
			},
			() => false,
			icon: "research"
		);
	}

	public override void Tick()
	{
		this.UpdateStateIfNecessary();

		var hasSelection = this.selectedActors.Length > 0;

		if (hasSelection && !this.hadSelection && this.sidebar.AnyCategoryOpen && !this.isBuildingSelection)
			this.sidebar.CloseAllBut(null);

		// Close production panel when the last building is deselected.
		if (!this.isBuildingSelection && this.wasBuildingSelection && this.sidebar.AnyCategoryOpen)
			this.sidebar.CloseAllBut(null);

		this.hadSelection = hasSelection;
		this.wasBuildingSelection = this.isBuildingSelection;

		if (this.deployHighlighted > 0)
			this.deployHighlighted--;

		if (this.scatterHighlighted > 0)
			this.scatterHighlighted--;

		if (this.stopHighlighted > 0)
			this.stopHighlighted--;

		if (this.selectedActors.Length == 0 || !this.RenderBounds.Contains(Viewport.LastMousePos))
			this.sidebar.IngameUi.Tooltip.Visible = false;

		base.Tick();
	}

	public override bool HandleKeyPress(KeyInput e)
	{
		if (this.selectedActors.Length == 0 || e.IsRepeat || e.Event != KeyInputEvent.Down)
			return false;

		for (var i = 0; i < this.slots.Length; i++)
		{
			if (this.slots[i] == null)
				continue;

			var hotkey = Game.ModData.Hotkeys[$"Production{i + 1:00}"].GetValue();

			if (e.Key != hotkey.Key || e.Modifiers != hotkey.Modifiers)
				continue;

			this.slots[i].OnClick();

			return true;
		}

		return false;
	}

	public override bool HandleMouseInput(MouseInput mi)
	{
		if (this.selectedActors.Length == 0)
			return false;

		if (mi.Event == MouseInputEvent.Move)
			this.UpdateTooltip(mi.Location);
		else if (mi.Event == MouseInputEvent.Down && mi.Button == MouseButton.Left)
		{
			for (var i = 0; i < this.slots.Length; i++)
			{
				var slot = this.slots[i];

				if (slot == null || slot.IsDisabled())
					continue;

				if (this.CellBounds(i).Contains(mi.Location))
				{
					slot.OnClick();

					return true;
				}
			}
		}

		return false;
	}

	public override void Draw()
	{
		if (this.selectedActors.Length == 0)
			return;

		for (var i = 0; i < this.slots.Length; i++)
		{
			var slot = this.slots[i];

			if (slot == null)
				continue;

			var bounds = this.CellBounds(i);
			var disabled = slot.IsDisabled();
			var highlighted = slot.IsHighlighted();

			this.sidebar.Buttons.PlayFetchIndex(highlighted ? "button-down" : "button", () => 0);
			WidgetUtils.DrawSpriteCentered(
				this.sidebar.Buttons.Image,
				this.sidebar.IngameUi.Palette,
				new(bounds.X + SidebarButtonWidget.Size / 2, bounds.Y + SidebarButtonWidget.Size / 2)
			);

			if (highlighted)
				WidgetUtils.FillRectWithColor(bounds, Color.FromArgb(64, 255, 255, 255));

			if (disabled)
				WidgetUtils.FillRectWithColor(bounds, Color.FromArgb(140, 0, 0, 0));

			if (slot.Icon != null)
			{
				this.sidebar.Buttons.PlayFetchIndex(slot.Icon, () => 0);
				WidgetUtils.DrawSpriteCentered(
					this.sidebar.Buttons.Image,
					this.sidebar.IngameUi.Palette,
					new(bounds.X + SidebarButtonWidget.Size / 2, bounds.Y + SidebarButtonWidget.Size / 2 - 4));
			}

			var labelFont = this.labelFont;
			if (labelFont != null)
			{
				var measure = labelFont.Measure(slot.Label);
				var textY = slot.Icon != null
					? bounds.Bottom - measure.Y - 2
					: bounds.Y + (SidebarButtonWidget.Size - measure.Y) / 2;

				labelFont.DrawTextWithContrast(
					slot.Label,
					new int2(bounds.X + (SidebarButtonWidget.Size - measure.X) / 2, textY),
					Color.White,
					Color.Black,
					1);
			}

			var keyFont = this.keyFont;
			if (keyFont != null)
			{
				var key = Game.ModData.Hotkeys[$"Production{i + 1:00}"].GetValue().DisplayString();
				var keyMeasure = keyFont.Measure(key);
				keyFont.DrawTextWithContrast(
					key,
					new int2(bounds.Right - 3 - keyMeasure.X, bounds.Bottom - 3 - keyMeasure.Y),
					Color.FromArgb(255, 200, 200, 200),
					Color.Black,
					1);
			}
		}
	}

	private Rectangle CellBounds(int index)
	{
		var x = index % SidebarWidget.GridColumns * SidebarButtonWidget.Size;
		var y = index / SidebarWidget.GridColumns * SidebarButtonWidget.Size;

		return new(this.RenderBounds.X + x, this.RenderBounds.Y + y, SidebarButtonWidget.Size, SidebarButtonWidget.Size);
	}

	private void UpdateTooltip(int2 location)
	{
		for (var i = 0; i < this.slots.Length; i++)
		{
			var slot = this.slots[i];

			if (slot == null || !this.CellBounds(i).Contains(location))
				continue;

			this.sidebar.IngameUi.Tooltip.TooltipTitle = slot.TooltipTitle;
			this.sidebar.IngameUi.Tooltip.TooltipText = slot.TooltipText;
			this.sidebar.IngameUi.Tooltip.Bounds = new(this.RenderBounds.X + this.Bounds.Width / 2, this.RenderBounds.Y + this.Bounds.Height / 2, 0, 0);
			this.sidebar.IngameUi.Tooltip.Visible = true;

			return;
		}
	}

	private void UpdateStateIfNecessary()
	{
		if (this.selectionHash == this.world.Selection.Hash)
			return;

		// Take all selected actors (units and buildings) and filter to just units for the unit commands.
		var allActors = this.world.Selection.Actors
			.Where(a => a.Owner == this.world.LocalPlayer && a.IsInWorld && !a.IsDead)
			.ToArray();

		var unitActors = allActors.Where(a => !a.Info.HasTraitInfo<BuildingInfo>()).ToArray();
		var buildingActors = allActors.Where(a => a.Info.HasTraitInfo<BuildingInfo>()).ToArray();

		// Show building commands only when solely buildings are selected.
		this.isBuildingSelection = buildingActors.Length > 0 && unitActors.Length == 0;

		if (this.isBuildingSelection)
		{
			this.selectedActors = buildingActors;
			this.PopulateBuildingSlots();
		}
		else
		{
			this.selectedActors = unitActors;
			this.PopulateUnitSlots();

			this.attackMoveDisabled = !this.selectedActors.Any(a =>
				a.Info.HasTraitInfo<AttackBaseInfo>() && a.Info.HasTraitInfo<IMoveInfo>() && a.Info.HasTraitInfo<AutoTargetInfo>());
			this.guardDisabled = !this.selectedActors.Any(a => a.Info.HasTraitInfo<GuardInfo>() && a.Info.HasTraitInfo<AutoTargetInfo>());
			this.scatterDisabled = !this.selectedActors.Any(a => a.Info.HasTraitInfo<IMoveInfo>());

			var cbbInfos = this.selectedActors.Select(a => a.Info.TraitInfoOrDefault<CommandBarBlacklistInfo>()).ToArray();
			this.stopDisabled = !cbbInfos.Any(i => i == null || !i.DisableStop);

			this.selectedDeploys = this.selectedActors
				.SelectMany(a => a.TraitsImplementing<IIssueDeployOrder>()
					.Select(d => new TraitPair<IIssueDeployOrder>(a, d)))
				.ToArray();

			this.actorStances = this.selectedActors
				.SelectMany(a => a.TraitsImplementing<AutoTarget>()
					.Where(at => at.Info.EnableStances)
					.Select(at => new TraitPair<AutoTarget>(a, at)))
				.ToArray();
		}

		this.selectionHash = this.world.Selection.Hash;
	}

	private bool GetStanceHighlighted(UnitStance stance)
	{
		this.UpdateStateIfNecessary();

		var active = this.actorStances.Where(at => !at.Trait.IsTraitDisabled).ToArray();

		return active.Length > 0 && active.All(at => at.Trait.PredictedStance == stance);
	}

	private void ToggleAttackMove(bool allowCancel)
	{
		if (this.world.OrderGenerator is AttackMoveOrderGenerator)
		{
			if (allowCancel)
				this.world.CancelInputMode();
		}
		else
			this.world.OrderGenerator = new AttackMoveOrderGenerator(this.selectedActors, Game.Settings.Game.MouseButtonPreference.Action);
	}

	private void ToggleGuard(bool allowCancel)
	{
		if (this.world.OrderGenerator is GuardOrderGenerator)
		{
			if (allowCancel)
				this.world.CancelInputMode();
		}
		else
			this.world.OrderGenerator = new GuardOrderGenerator(
				this.selectedActors,
				"Guard",
				"guard",
				Game.Settings.Game.MouseButtonPreference.Action
			);
	}

	private void PerformKeyboardOrderOnSelection(Func<Actor, Order> f)
	{
		this.UpdateStateIfNecessary();

		var orders = this.selectedActors.Select(f).ToArray();

		foreach (var order in orders)
			this.world.IssueOrder(order);

		orders.PlayVoiceForOrders();
	}

	private void PerformDeployOrderOnSelection(bool queued)
	{
		this.UpdateStateIfNecessary();

		var orders = this.selectedDeploys
			.Where(pair => pair.Trait.CanIssueDeployOrder(pair.Actor, queued))
			.Select(pair => pair.Trait.IssueDeployOrder(pair.Actor, queued))
			.Where(order => order != null)
			.ToArray();

		foreach (var order in orders)
			this.world.IssueOrder(order);

		orders.PlayVoiceForOrders();
	}

	private void SetSelectionStance(UnitStance stance)
	{
		this.UpdateStateIfNecessary();

		foreach (var pair in this.actorStances)
		{
			if (!pair.Trait.IsTraitDisabled)
				pair.Trait.PredictedStance = stance;

			this.world.IssueOrder(new Order("SetUnitStance", pair.Actor, false) { ExtraData = (uint)stance });
		}
	}
}

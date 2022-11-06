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

using Common.Orders;
using Common.Traits;
using Graphics;
using JetBrains.Annotations;
using Mechanics.Docking;
using Mechanics.Oil.Activities;
using Mechanics.Oil.Traits;
using OpenRA.Widgets;

[UsedImplicitly]

// TODO add all the hotkeys we need for stances etc here, so we can remove the trash from the ingame chrome yaml.
public sealed class IngameUiWidget : Widget
{
	public readonly World World;
	public readonly WorldRenderer WorldRenderer;
	public readonly RadarPings RadarPings;

	public readonly PaletteReference Palette;

	public readonly RadarWidget Radar;
	public readonly TooltipWidget Tooltip;

	private ISound? oilSound;

	[ObjectCreator.UseCtorAttribute]
	public IngameUiWidget(World world, WorldRenderer worldRenderer)
	{
		this.World = world;
		this.WorldRenderer = worldRenderer;
		this.RadarPings = world.WorldActor.TraitOrDefault<RadarPings>();

		this.IgnoreChildMouseOver = true;
		this.IgnoreMouseOver = true;

		this.Palette = this.WorldRenderer.Palette($"player{this.World.LocalPlayer.InternalName}");

		this.AddChild(new StatusWidget(this));
		this.AddChild(this.Radar = new(this));
		this.AddChild(new SidebarWidget(this));
		this.AddChild(this.Tooltip = new());

		this.Resize();
		this.TickOuter();
	}

	private void Resize()
	{
		this.Bounds = new(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);
	}

	public override void Tick()
	{
		var playOilSound = this.World.Actors.Any(
			a =>
			{
				if (a.IsDead || !a.IsInWorld || a.Owner != a.World.LocalPlayer)
					return false;

				var tanker = a.TraitOrDefault<Tanker>();

				if (tanker == null)
					return false;

				if (a.CurrentActivity is not TankerCycle tankerCycle)
					return false;

				return tankerCycle.DockingState == DockingState.Docked
					&& tankerCycle.DockActor != null
					&& tankerCycle.DockActor.Info.HasTraitInfo<PowerStationInfo>();
			}
		);

		switch (playOilSound)
		{
			case true when this.oilSound == null:
				this.oilSound = Game.Sound.PlayLooped(SoundType.UI, "191.wav"); // TODO un-hardcode this!

				break;

			case false when this.oilSound != null:
				Game.Sound.StopSound(this.oilSound);
				this.oilSound = null;

				break;
		}
	}

	public override bool HandleKeyPress(KeyInput e)
	{
		if (e.Key != Game.ModData.Hotkeys["PlaceBeacon"].GetValue().Key)
			return false;

		this.World.OrderGenerator = new BeaconOrderGenerator();

		return true;
	}
}

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
	using System.Linq;
	using Common.Orders;
	using Common.Traits;
	using Mechanics.Docking;
	using Mechanics.Docking.Traits.Actions;
	using Mechanics.Oil.Activities;
	using Mechanics.Oil.Traits;
	using OpenRA.Graphics;
	using OpenRA.Widgets;
	using Primitives;

	// TODO add all the hotkeys we need for stances etc here, so we can remove the trash from the ingame chrome yaml.
	public sealed class IngameUiWidget : Widget
	{
		public readonly World World;
		public readonly WorldRenderer WorldRenderer;
		public readonly RadarPings RadarPings;

		public readonly PaletteReference Palette;

		public readonly RadarWidget Radar;
		public readonly TooltipWidget Tooltip;

		private ISound oilSound;

		[ObjectCreator.UseCtorAttribute]
		public IngameUiWidget(World world, WorldRenderer worldRenderer)
		{
			World = world;
			WorldRenderer = worldRenderer;
			RadarPings = world.WorldActor.Trait<RadarPings>();

			IgnoreChildMouseOver = true;
			IgnoreMouseOver = true;

			Palette = WorldRenderer.Palette($"player{World.LocalPlayer.InternalName}");

			AddChild(new StatusWidget(this));
			AddChild(Radar = new RadarWidget(this));
			AddChild(new SidebarWidget(this));
			AddChild(Tooltip = new TooltipWidget());

			Resize();
			TickOuter();
		}

		public void Resize()
		{
			Bounds = new Rectangle(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);
		}

		public override void Tick()
		{
			var playOilSound = World.Actors.Any(
				a =>
				{
					if (a.IsDead || !a.IsInWorld || a.Owner != a.World.LocalPlayer)
						return false;

					var tanker = a.TraitOrDefault<Tanker>();

					if (tanker == null)
						return false;

					if (!(a.CurrentActivity is TankerCycle tankerCycle))
						return false;

					return tankerCycle.DockingState == DockingState.Docked && tankerCycle.DockActor.Info.HasTraitInfo<PowerStationInfo>();
				});

			if (playOilSound && oilSound == null)
				oilSound = Game.Sound.PlayLooped(SoundType.UI, "191.wav"); // TODO un-hardcode this!
			else if (!playOilSound && oilSound != null)
			{
				Game.Sound.StopSound(oilSound);
				oilSound = null;
			}
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Key != Game.ModData.Hotkeys["PlaceBeacon"].GetValue().Key)
				return false;

			World.OrderGenerator = new BeaconOrderGenerator();

			return true;
		}
	}
}

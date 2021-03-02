#region Copyright & License Information
/*
 * Copyright 2007-2021 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Activities;
using OpenRA.Mods.Kknd.Traits.Resources;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets.Ingame
{
	// TODO add all the hotkeys we need for stances etc here, so we can remove the trash from the ingame chrome yaml.
	public sealed class IngameUiWidget : Widget
	{
		public readonly World World;
		public readonly WorldRenderer WorldRenderer;
		public readonly RadarPings RadarPings;

		public readonly PaletteReference Palette;

		public readonly StatusWidget Status;
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

			Palette = WorldRenderer.Palette("player" + World.LocalPlayer.InternalName);

			AddChild(Status = new StatusWidget(this));
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

		public override string GetCursor(int2 pos) { return null; }

		public override void Tick()
		{
			var playOilSound = World.Actors.Any(a =>
			{
				if (a.IsDead || !a.IsInWorld || a.Owner != a.World.LocalPlayer)
					return false;

				var tanker = a.TraitOrDefault<Tanker>();

				if (tanker == null)
					return false;

				var dockActivity = a.CurrentActivity as Docking;

				if (dockActivity == null)
					return false;

				return dockActivity.DockingState == DockingState.Docked && dockActivity.DockActor.Info.HasTraitInfo<PowerStationInfo>();
			});

			if (playOilSound && oilSound == null)
				oilSound = Game.Sound.PlayLooped(SoundType.UI, "191.wav"); // TODO un-hardcode this for kknd2!
			else if (!playOilSound && oilSound != null)
			{
				Game.Sound.StopSound(oilSound);
				oilSound = null;
			}
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Key == Game.ModData.Hotkeys["PlaceBeacon"].GetValue().Key)
			{
				World.OrderGenerator = new BeaconOrderGenerator();
				return true;
			}

			return false;
		}
	}
}

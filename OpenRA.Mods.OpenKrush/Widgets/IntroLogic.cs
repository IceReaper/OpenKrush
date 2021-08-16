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

namespace OpenRA.Mods.OpenKrush.Widgets
{
	using Common.Traits;
	using FileFormats;
	using GameProviders;
	using GameRules;
	using OpenRA.Widgets;
	using Primitives;

	public class IntroLogic : ChromeLogic
	{
		private static int state;

		private readonly Widget widget;
		private readonly ModData modData;
		private readonly World world;
		private readonly VbcPlayerWidget player;
		private readonly MusicInfo song;

		[ObjectCreator.UseCtorAttribute]
		public IntroLogic(Widget widget, World world, ModData modData)
		{
			if (IntroLogic.state != 0)
				return;

			this.widget = widget;
			this.world = world;
			this.modData = modData;

			widget.AddChild(player = new VbcPlayerWidget(this.modData));

			player.Bounds = new Rectangle(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);

			var musicPlayList = world.WorldActor.Trait<MusicPlaylist>();
			song = musicPlayList.CurrentSong();
			musicPlayList.Stop();
		}

		public override void Tick()
		{
			if (IntroLogic.state == 0)
				PlayVideo(GameProvider.Movies.ContainsKey("mh_fmv.vbc") ? GameProvider.Movies["mh_fmv.vbc"] : GameProvider.Movies["mh.vbc"]);
			else if (IntroLogic.state == 2)
				PlayVideo(GameProvider.Movies["intro.vbc"]);
			else if (IntroLogic.state == 4)
			{
				widget.RemoveChild(player);
				IntroLogic.state++;

				if (song != null)
					world.WorldActor.Trait<MusicPlaylist>().Play(song);
			}
		}

		private void PlayVideo(string video)
		{
			IntroLogic.state++;

			if (!modData.ModFiles.TryOpen(video, out var stream))
			{
				IntroLogic.state++;

				return;
			}

			player.Video = new Vbc(stream);
			player.Play(() => IntroLogic.state++);
		}
	}
}

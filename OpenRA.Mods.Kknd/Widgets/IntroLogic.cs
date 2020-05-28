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

using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Kknd.FileFormats;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets
{
	public class IntroLogic : ChromeLogic
	{
		private static int state;

		private readonly Widget widget;
		private readonly ModData modData;
		private readonly World world;
		private readonly VbcPlayerWidget player;
		private readonly MusicPlaylist musicPlayList;
		private MusicInfo song;

		[ObjectCreator.UseCtor]
		public IntroLogic(Widget widget, World world, ModData modData)
		{
			if (state != 0)
				return;

			this.widget = widget;
			this.world = world;
			this.modData = modData;

			widget.AddChild(player = new VbcPlayerWidget());

			player.Bounds = new Rectangle(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);

			musicPlayList = world.WorldActor.Trait<MusicPlaylist>();
			song = musicPlayList.CurrentSong();
			musicPlayList.Stop();
		}

		public override void Tick()
		{
			if (state == 0)
				PlayVideo("Melbourne House");
			else if (state == 2)
				PlayVideo("Intro");
			else if (state == 4)
			{
				widget.RemoveChild(player);
				state++;

				if (song != null)
					world.WorldActor.Trait<MusicPlaylist>().Play(song);
			}
		}

		private void PlayVideo(string video)
		{
			state++;

			if (!modData.ModFiles.Exists("content|Movies/" + video + ".vbc"))
			{
				state++;
				return;
			}

			player.Video = new Vbc(modData.ModFiles.Open("content|Movies/" + video + ".vbc"));
			player.Play(() => state++);
		}
	}
}

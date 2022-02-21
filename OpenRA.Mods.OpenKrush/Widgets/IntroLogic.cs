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

namespace OpenRA.Mods.OpenKrush.Widgets;

using Common.Traits;
using GameRules;
using JetBrains.Annotations;
using OpenRA.Widgets;

[UsedImplicitly]
public class IntroLogic : ChromeLogic
{
	private static int state;

	private readonly Widget? widget;
	private readonly ModData? modData;
	private readonly World? world;
	private readonly VbcPlayerWidget? player;
	private readonly MusicInfo? song;

	[ObjectCreator.UseCtorAttribute]
	public IntroLogic(Widget widget, World world, ModData modData)
	{
		if (IntroLogic.state != 0)
			return;

		this.widget = widget;
		this.world = world;
		this.modData = modData;

		widget.AddChild(this.player = new(this.modData));

		this.player.Bounds = new(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);

		var musicPlayList = world.WorldActor.TraitOrDefault<MusicPlaylist>();
		this.song = musicPlayList.CurrentSong();
		musicPlayList.Stop();
	}

	public override void Tick()
	{
		if (this.modData == null)
		{
			this.Finished();

			return;
		}

		switch (IntroLogic.state)
		{
			case 0:
				this.PlayVideo(
					this.modData.Manifest.Id switch
					{
						"openkrush_gen1" => "mh_fmv.vbc",
						"openkrush_gen2" => "mh.vbc",
						_ => null
					}
				);

				break;

			case 2:
				this.PlayVideo("intro.vbc");

				break;

			case 4:
			{
				this.Finished();

				break;
			}
		}
	}

	private void PlayVideo(string? video)
	{
		IntroLogic.state++;

		if (video == null || this.player == null)
		{
			IntroLogic.state++;

			return;
		}

		var fmvPackage = this.modData?.ModFiles.MountedPackages.FirstOrDefault(
			package => this.modData.ModFiles.GetPrefix(package)
				== this.modData.Manifest.Id switch
				{
					"openkrush_gen1" => "gen1_fmv",
					"openkrush_gen2" => "gen2_fmv",
					_ => null
				}
		);

		var stream = fmvPackage?.Contents.Where(file => file.Equals(video, StringComparison.InvariantCultureIgnoreCase))
			.Select(file => fmvPackage.GetStream(file))
			.FirstOrDefault();

		if (stream == null)
		{
			IntroLogic.state++;

			return;
		}

		this.player.Video = new(stream);
		this.player.Play(() => IntroLogic.state++);
	}

	private void Finished()
	{
		this.widget?.RemoveChild(this.player);
		IntroLogic.state++;

		if (this.song != null)
			this.world?.WorldActor.TraitOrDefault<MusicPlaylist>().Play(this.song);
	}
}

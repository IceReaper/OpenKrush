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
	using System;
	using Common.Widgets;
	using FileFormats;
	using OpenRA.Graphics;
	using Primitives;

	public class VbcPlayerWidget : ColorBlockWidget
	{
		private Vbc video;

		private Sprite videoSprite;

		private long duration;
		private long start;
		private int lastFrame;
		private Action onComplete;

		public Vbc Video
		{
			get => video;

			set
			{
				video = value;

				var size = new Size(Exts.NextPowerOf2(video.Width), Exts.NextPowerOf2(video.Height));
				videoSprite = new Sprite(new Sheet(SheetType.BGRA, size), new Rectangle(0, 0, size.Width, size.Height), TextureChannel.RGBA);
				videoSprite.Sheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;
			}
		}

		public VbcPlayerWidget()
		{
			Visible = false;
			Color = Color.Black;
		}

		public void Play(Action onComplete)
		{
			this.onComplete = onComplete;
			Visible = true;

			start = Game.RunTime;
			lastFrame = 0;
			UpdateFrame();

			var audio = Video.AudioData;
			duration = audio.Length * 1000L / (video.SampleRate * 1 * (video.SampleBits / 8));
			Game.Sound.PlayVideo(audio, 1, Video.SampleBits, Video.SampleRate);
		}

		private void UpdateFrame()
		{
			Video.AdvanceFrame();
			videoSprite.Sheet.GetTexture().SetData(Video.FrameData);
		}

		public void Stop()
		{
			Game.Sound.StopVideo();
			Visible = false;
			onComplete();
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
				Stop();

			return true;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down)
				Stop();

			return true;
		}

		public override void Tick()
		{
			if (!Visible)
				return;

			if (start + duration < Game.RunTime)
			{
				Stop();

				return;
			}

			var currentFrame = Math.Min((Game.RunTime - start) * Video.Frames / duration, Video.Frames);

			while (currentFrame > lastFrame)
			{
				++lastFrame;

				if (lastFrame < video.Frames)
					UpdateFrame();
			}
		}

		public override void Draw()
		{
			if (!Visible)
				return;

			base.Draw();

			var scale = Math.Min(Bounds.Width / video.Width, Bounds.Height / video.Height);
			var videoSize = new int2(video.Width * scale, video.Height * scale);
			var sheetSize = new int2(videoSprite.Sheet.Size.Width * scale, videoSprite.Sheet.Size.Height * scale);
			var position = new int2((Bounds.Width - videoSize.X) / 2, (Bounds.Height - videoSize.Y) / 2) + Bounds.Location;

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(videoSprite, position, sheetSize);
		}
	}
}

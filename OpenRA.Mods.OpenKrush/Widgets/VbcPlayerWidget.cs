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

namespace OpenRA.Mods.OpenKrush.Widgets;

using Assets.FileFormats;
using Common.Widgets;
using Graphics;
using Primitives;

public class VbcPlayerWidget : ColorBlockWidget
{
	private Vbc? video;

	private Sprite? videoSprite;

	private long duration;
	private long start;
	private int lastFrame;
	private Action? onComplete;

	public Vbc? Video
	{
		set
		{
			this.video = value;

			if (this.video == null)
			{
				this.videoSprite = null;

				return;
			}

			var size = new Size(Exts.NextPowerOf2(this.video.Width), Exts.NextPowerOf2(this.video.Height));
			this.videoSprite = new(new(SheetType.BGRA, size), new(0, 0, size.Width, size.Height), TextureChannel.RGBA);
			this.videoSprite.Sheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;
		}
	}

	public VbcPlayerWidget(ModData modData)
		: base(modData)
	{
		this.Visible = false;
		this.Color = Color.Black;
	}

	public void Play(Action complete)
	{
		this.onComplete = complete;
		this.Visible = true;

		this.start = Game.RunTime;
		this.lastFrame = 0;
		this.UpdateFrame();

		if (this.video == null)
			return;

		var audio = this.video.AudioData;
		this.duration = audio.Length * 1000L / (this.video.SampleRate * 1 * (this.video.SampleBits / 8));
		Game.Sound.PlayVideo(audio, 1, this.video.SampleBits, this.video.SampleRate);
	}

	private void UpdateFrame()
	{
		if (this.video == null)
			return;

		this.video.AdvanceFrame();

		this.videoSprite?.Sheet.GetTexture().SetData(this.video.CurrentFrameData, Exts.NextPowerOf2(this.video.Width), Exts.NextPowerOf2(this.video.Height));
	}

	private void Stop()
	{
		Game.Sound.StopVideo();
		this.Visible = false;
		this.onComplete?.Invoke();
	}

	public override bool HandleKeyPress(KeyInput e)
	{
		if (e.Event == KeyInputEvent.Down)
			this.Stop();

		return true;
	}

	public override bool HandleMouseInput(MouseInput mi)
	{
		if (mi.Event == MouseInputEvent.Down)
			this.Stop();

		return true;
	}

	public override void Tick()
	{
		if (!this.Visible)
			return;

		if (this.start + this.duration < Game.RunTime)
		{
			this.Stop();

			return;
		}

		if (this.video == null)
			return;

		var currentFrame = Math.Min((Game.RunTime - this.start) * this.video.FrameCount / this.duration, this.video.FrameCount);

		while (currentFrame > this.lastFrame)
		{
			++this.lastFrame;

			if (this.lastFrame < this.video.FrameCount)
				this.UpdateFrame();
		}
	}

	public override void Draw()
	{
		if (!this.Visible)
			return;

		base.Draw();

		if (this.video == null)
			return;

		var scale = Math.Min(this.Bounds.Width / this.video.Width, this.Bounds.Height / this.video.Height);
		var videoSize = new int2(this.video.Width * scale, this.video.Height * scale);
		var position = new int2((this.Bounds.Width - videoSize.X) / 2, (this.Bounds.Height - videoSize.Y) / 2) + this.Bounds.Location;

		Game.Renderer.RgbaSpriteRenderer.DrawSprite(this.videoSprite, position, scale);
	}
}

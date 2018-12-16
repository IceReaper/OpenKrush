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

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Kknd.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets
{
    public class VbcPlayerWidget : Widget
    {
        private Vbc video;
        private byte[] frame;
        private uint[] palette;

        private Sprite videoSprite;

        private long duration;
        private long start;
        private int lastFrame;
        private Action onComplete;

        public Vbc Video
        {
            get { return video; }
            set
            {
                video = value;
                frame = new byte[video.Size.X * video.Size.Y];
                palette = new uint[256];

                var size = new Size(Exts.NextPowerOf2(video.Size.X), Exts.NextPowerOf2(video.Size.Y));
                videoSprite = new Sprite(new Sheet(SheetType.BGRA, size), new Rectangle(0, 0, size.Width, size.Height), TextureChannel.RGBA);
                videoSprite.Sheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;
            }
        }

        public VbcPlayerWidget()
        {
            Visible = false;
        }

        public void Play(Action onComplete)
        {
            this.onComplete = onComplete;
            Visible = true;

            start = Game.RunTime;
            lastFrame = 0;
            UpdateFrame();

            var audio = Video.GetAudio();
            duration = audio.Length * 1000L / (video.SampleRate * 1 * (video.SampleBits / 8));
            Game.Sound.PlayVideo(audio, 1, Video.SampleBits, Video.SampleRate);
        }

        private void UpdateFrame()
        {
            frame = Video.ApplyFrame(lastFrame, frame, palette);
            var data = new uint[videoSprite.Sheet.Size.Height, videoSprite.Sheet.Size.Width];

            for (var i = 0; i < frame.Length; i++)
                data[i / video.Size.X, i % video.Size.X] = palette[frame[i]];

            videoSprite.Sheet.GetTexture().SetData(data);
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
                UpdateFrame();
            }
        }

        public override void Draw()
        {
            if (!Visible)
                return;

            var yFactor = video.Size.Y == 240 ? 2 : 1;
            var scale = Math.Min(Game.Renderer.Resolution.Width / video.Size.X, Game.Renderer.Resolution.Height / (video.Size.Y * yFactor));
            var videoSize = new int2(video.Size.X * scale, video.Size.Y * yFactor * scale);
            var sheetSize = new int2(videoSprite.Sheet.Size.Width * scale, videoSprite.Sheet.Size.Height * yFactor * scale);
            var position = new int2((Game.Renderer.Resolution.Width - videoSize.X) / 2, (Game.Renderer.Resolution.Height - videoSize.Y) / 2);

            Game.Renderer.RgbaSpriteRenderer.DrawSprite(videoSprite, position, sheetSize);
        }
    }
}

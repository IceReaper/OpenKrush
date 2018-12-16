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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Kknd.FileFormats;
using OpenRA.Mods.Kknd.FileSystem;
using OpenRA.Mods.Kknd.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Kknd.SpriteLoaders
{
	public class MobdLoader : ISpriteLoader
	{
		private class MobdSpriteFrame : ISpriteFrame
		{
			public Size Size { get; private set; }
			public Size FrameSize { get; private set; }
			public float2 Offset { get; private set; }
			public byte[] Data { get; private set; }
			public readonly uint[] Palette;
			public readonly MobdPoint[] Points;

			public bool DisableExportPadding { get { return true; } }

			public MobdSpriteFrame(MobdFrame mobdFrame)
			{
				var width = mobdFrame.RenderFlags.Image.Width;
				var height = mobdFrame.RenderFlags.Image.Height;
				var x = mobdFrame.OriginX;
				var y = mobdFrame.OriginY;

				Size = new Size((int)width, (int)height);
				FrameSize = new Size((int)width, (int)height);
				Offset = new int2((int)(width / 2 - x), (int)(height / 2 - y));
				Data = mobdFrame.RenderFlags.Image.Pixels;
				Palette = mobdFrame.RenderFlags.Palette;
				Points = mobdFrame.Points;
			}
		}

		bool IsMobd(Stream stream, out Version version)
		{
			version = Version.UNKNOWN;

			var innerStream = stream as SegmentStream;
			if (innerStream == null)
				return false;

			var outerStream = innerStream.BaseStream as SegmentStream;
			if (outerStream == null)
				return false;

			var originalPosition = outerStream.BaseStream.Position;
			outerStream.BaseStream.Position = 0;
			var magic = outerStream.BaseStream.ReadASCII(4);
			outerStream.BaseStream.Position = originalPosition;

			switch (magic)
			{
				case "DATA":
					version = Version.KKND1;
					return true;
				
				case "DAT2":
					version = Version.KKND2;
					return true;
				
				default:
					return false;
			}
		}

		public bool TryParseSprite(Stream stream, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			Version version;

			if (!IsMobd(stream, out version))
			{
				metadata = null;
				frames = null;
				return false;
			}

			var mobd = new Mobd(stream as SegmentStream, version);
			var tmp = new List<MobdSpriteFrame>();

			tmp.AddRange(from mobdAnimation in mobd.Animations
				from mobdFrame in mobdAnimation.Frames
				select new MobdSpriteFrame(mobdFrame));
			tmp.AddRange(from mobdAnimation in mobd.HardcodedAnimations
				from mobdFrame in mobdAnimation.Frames
				select new MobdSpriteFrame(mobdFrame));
			
			uint[] palette = null;
			var points = new Dictionary<int, Offset[]>();

			for (var i = 0; i < tmp.Count; i++)
			{
				if (tmp[i].Points != null)
					points.Add(i, tmp[i].Points.Select(point => new Offset {Id = point.Id, X = point.X, Y = point.Y}).ToArray());

				if (tmp[i].Palette != null)
					palette = tmp[i].Palette;
			}

			frames = tmp.ToArray();

			metadata = new TypeDictionary {new EmbeddedSpritePalette(palette), new EmbeddedSpriteOffsets(points)};

			return true;
		}
	}
}

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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Kknd.Graphics;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Projectiles
{
	[Desc("A beautiful generated laser beam.")]
	public class LaserInfo : IProjectileInfo
	{
		[Desc("The maximum duration (in ticks) of the beam's existence.")]
		public readonly int Duration = 10;

		[Desc("Color of the beam. Default falls back to player color.")]
		public readonly Color Color = Color.Transparent;

		[Desc("Inner lightness of the beam.")]
		public readonly byte InnerLightness = 0xff;

		[Desc("Outer lightness of the beam.")]
		public readonly byte OuterLightness = 0x80;

		[Desc("The radius of the beam.")]
		public readonly int Radius = 3;

		[Desc("Distortion offset.")]
		public readonly int Distortion = 0;

		[Desc("Distortion animation offset.")]
		public readonly int DistortionAnimation = 0;

		[Desc("Maximum length per segment.")]
		public readonly WDist SegmentLength = WDist.Zero;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		public IProjectile Create(ProjectileArgs args) { return new Laser(args, this); }
	}

	public class Laser : IProjectile
	{
		readonly LaserInfo info;
		readonly Color[] colors;
		readonly WPos[] offsets;

		readonly WVec leftVector;
		readonly WVec upVector;
		readonly MersenneTwister random;

		int ticks;

		public Laser(ProjectileArgs args, LaserInfo info)
		{
			this.info = info;

			colors = new Color[info.Radius];
			for (var i = 0; i < info.Radius; i++)
			{
				var color = info.Color == Color.Transparent ? args.SourceActor.Owner.Color : info.Color;
				var bw = (float)((info.InnerLightness - info.OuterLightness) * i / (info.Radius - 1) + info.OuterLightness) / 0xff;
				var dstR = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.R / 0xff) : 2 * bw * ((float)color.R / 0xff);
				var dstG = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.G / 0xff) : 2 * bw * ((float)color.G / 0xff);
				var dstB = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.B / 0xff) : 2 * bw * ((float)color.B / 0xff);
				colors[i] = Color.FromArgb((int)(dstR * 0xff), (int)(dstG * 0xff), (int)(dstB * 0xff));
			}

			var direction = args.PassiveTarget - args.Source;

			if (info.Distortion != 0 || info.DistortionAnimation != 0)
			{
				leftVector = new WVec(direction.Y, -direction.X, 0);
				if (leftVector.Length != 0)
					leftVector = 1024 * leftVector / leftVector.Length;

				upVector = new WVec(
					-direction.X * direction.Z,
					-direction.Z * direction.Y,
					direction.X * direction.X + direction.Y * direction.Y);
				if (upVector.Length != 0)
					upVector = 1024 * upVector / upVector.Length;

				random = args.SourceActor.World.SharedRandom;
			}

			if (this.info.SegmentLength == WDist.Zero)
			{
				offsets = new[] { args.Source, args.PassiveTarget };
			}
			else
			{
				var numSegments = (direction.Length - 1) / info.SegmentLength.Length + 1;
				offsets = new WPos[numSegments + 1];
				offsets[0] = args.Source;
				offsets[offsets.Length - 1] = args.PassiveTarget;

				for (var i = 1; i < numSegments; i++)
				{
					var segmentStart = direction / numSegments * i;
					offsets[i] = args.Source + segmentStart;

					if (info.Distortion != 0)
					{
						var angle = WAngle.FromDegrees(random.Next(360));
						var distortion = random.Next(info.Distortion);

						var offset = distortion * angle.Cos() * leftVector / (1024 * 1024)
							+ distortion * angle.Sin() * upVector / (1024 * 1024);

						offsets[i] += offset;
					}
				}
			}

			args.Weapon.Impact(Target.FromPos(args.PassiveTarget), args.SourceActor);
		}

		public void Tick(World world)
		{
			if (++ticks >= info.Duration)
				world.AddFrameEndTask(w => w.Remove(this));
			else if (info.DistortionAnimation != 0)
			{
				for (var i = 1; i < offsets.Length - 1; i++)
				{
					var angle = WAngle.FromDegrees(random.Next(360));
					var distortion = random.Next(info.DistortionAnimation);

					var offset = distortion * angle.Cos() * leftVector / (1024 * 1024)
						+ distortion * angle.Sin() * upVector / (1024 * 1024);

					offsets[i] += offset;
				}
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer worldRenderer)
		{
			for (var i = 0; i < offsets.Length - 1; i++)
				for (var j = 0; j < info.Radius; j++)
					yield return new LaserRenderable(offsets, info.ZOffset, new WDist(32 + (info.Radius - j - 1) * 64), colors[j]);
		}
	}
}
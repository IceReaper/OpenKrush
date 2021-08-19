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

namespace OpenRA.Mods.OpenKrush.Mechanics.Laser.Projectiles
{
	using GameRules;
	using Graphics;
	using JetBrains.Annotations;
	using OpenRA.Graphics;
	using Primitives;
	using Support;
	using System;
	using System.Collections.Generic;
	using System.Numerics;
	using Traits;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
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
		public readonly int Distortion;

		[Desc("Distortion animation offset.")]
		public readonly int DistortionAnimation;

		[Desc("Maximum length per segment.")]
		public readonly WDist SegmentLength = WDist.Zero;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset;

		public IProjectile Create(ProjectileArgs args)
		{
			return new Laser(args, this);
		}
	}

	public class Laser : IProjectile
	{
		private readonly LaserInfo info;
		private readonly MersenneTwister random;
		private readonly Color[] colors;
		private readonly WPos[] offsets;
		private readonly float[]? distances;

		private int ticks;

		public Laser(ProjectileArgs args, LaserInfo info)
		{
			this.info = info;
			this.random = args.SourceActor.World.SharedRandom;

			this.colors = new Color[info.Radius];

			for (var i = 0; i < info.Radius; i++)
			{
				var color = info.Color == Color.Transparent ? args.SourceActor.Owner.Color : info.Color;
				var bw = (float)((info.InnerLightness - info.OuterLightness) * i / (info.Radius - 1) + info.OuterLightness) / 0xff;
				var dstR = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.R / 0xff) : 2 * bw * ((float)color.R / 0xff);
				var dstG = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.G / 0xff) : 2 * bw * ((float)color.G / 0xff);
				var dstB = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.B / 0xff) : 2 * bw * ((float)color.B / 0xff);
				this.colors[i] = Color.FromArgb((int)(dstR * 0xff), (int)(dstG * 0xff), (int)(dstB * 0xff));
			}

			var direction = args.PassiveTarget - args.Source;

			if (this.info.SegmentLength == WDist.Zero)
				this.offsets = new[] { args.Source, args.PassiveTarget };
			else
			{
				var numSegments = (direction.Length - 1) / info.SegmentLength.Length + 1;

				this.offsets = new WPos[numSegments + 1];
				this.offsets[0] = args.Source;
				this.offsets[^1] = args.PassiveTarget;

				this.distances = new float[this.offsets.Length];

				if (info.Distortion != 0)
				{
					for (var i = 1; i < numSegments; i++)
						this.distances[i] = this.random.Next(info.Distortion * 2) - info.Distortion;
				}

				this.CalculateOffsets();
			}

			args.Weapon.Impact(Target.FromPos(args.PassiveTarget), args.SourceActor);
		}

		public void Tick(World world)
		{
			if (++this.ticks >= this.info.Duration)
				world.AddFrameEndTask(w => w.Remove(this));
			else if (this.info.DistortionAnimation != 0 && this.distances != null)
			{
				for (var i = 1; i < this.distances.Length - 1; i++)
				{
					this.distances[i] = Math.Clamp(
						this.distances[i] + this.random.Next(this.info.DistortionAnimation * 2) - this.info.DistortionAnimation,
						-this.info.Distortion,
						this.info.Distortion
					);
				}

				this.CalculateOffsets();
			}
		}

		private void CalculateOffsets()
		{
			if (this.distances == null)
				return;

			var source = this.offsets[0];
			var numSegments = this.offsets.Length - 1;
			var distance = (this.offsets[numSegments] - source) / numSegments;

			for (var i = 1; i < numSegments; i++)
				this.offsets[i] = source + Laser.FindPoint(distance * i, this.distances[i]);
		}

		private static WVec FindPoint(WVec pos, float distance)
		{
			// TODO remove System.Numerics here.
			var dir = new Vector3(pos.X, pos.Y, pos.Z);
			var circumference = 2 * (float)Math.PI * dir.Length();
			var angle = distance / circumference * (float)Math.PI;
			dir = Vector3.Transform(dir, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle));

			return new((int)dir.X, (int)dir.Y, (int)dir.Z);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer worldRenderer)
		{
			for (var i = 0; i < this.offsets.Length - 1; i++)
			for (var j = 0; j < this.info.Radius; j++)
				yield return new LaserRenderable(this.offsets, this.info.ZOffset, new(32 + (this.info.Radius - j - 1) * 64), this.colors[j]);
		}
	}
}

using System.Collections.Generic;
using System.Drawing;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Kknd.Graphics;
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

        [Desc("Disortion offset.")]
        public readonly int Distortion = 0;

        [Desc("Disortion animation offset.")]
        public readonly int DistortionAnimation = 0;

        [Desc("Maximum length per segment.")]
        public readonly int SegmentLength = 0;

        [Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
        public readonly int ZOffset = 0;

        public IProjectile Create(ProjectileArgs args) { return new Laser(args, this); }
    }

    public class Laser : IProjectile
    {
        private readonly LaserInfo info;
        private readonly Color[] colors;
        private readonly int2[] offsets;

        private int ticks;

        public Laser(ProjectileArgs args, LaserInfo info)
        {
            this.info = info;

            colors = new Color[info.Radius];
            for (var i = 0; i < info.Radius; i++)
            {
                var color = info.Color == Color.Transparent ? args.SourceActor.Owner.Color.RGB : info.Color;
                var bw = (float) ((info.InnerLightness - info.OuterLightness) * i / (info.Radius - 1) + info.OuterLightness) / 0xff;
                var dstR = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float) color.R / 0xff) : 2 * bw * ((float) color.R / 0xff);
                var dstG = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float) color.G / 0xff) : 2 * bw * ((float) color.G / 0xff);
                var dstB = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float) color.B / 0xff) : 2 * bw * ((float) color.B / 0xff);
                colors[i] = Color.FromArgb((int) (dstR * 0xff), (int) (dstG * 0xff), (int) (dstB * 0xff));
            }

            var direction = args.PassiveTarget - args.Source;
            if (this.info.SegmentLength == 0)
            {
                offsets = new[] {new int2(args.Source.X, args.Source.Y), new int2(args.PassiveTarget.X, args.PassiveTarget.Y)};
            }
            else
            {
                var numSegments = (direction.Length - 1) / info.SegmentLength + 1;
                offsets = new int2[numSegments + 1];
                offsets[0] = new int2(args.Source.X, args.Source.Y);
                offsets[offsets.Length - 1] = new int2(args.PassiveTarget.X, args.PassiveTarget.Y);

                for (var i = 1; i < numSegments; i++)
                {
                    var segmentStart = direction / numSegments * i;
                    offsets[i] = new int2(args.Source.X + segmentStart.X, args.Source.Y + segmentStart.Y);

					if (info.Distortion != 0)
                    {
                        offsets[i] = new int2(
                            offsets[i].X + Game.CosmeticRandom.Next(-info.Distortion / 2, info.Distortion / 2),
                            offsets[i].Y + Game.CosmeticRandom.Next(-info.Distortion / 2, info.Distortion / 2)
                        );
                    }
                }
            }

            args.Weapon.Impact(Target.FromPos(args.PassiveTarget), args.SourceActor, args.DamageModifiers);
        }

        public void Tick(World world)
        {
            if (++ticks >= info.Duration)
                world.AddFrameEndTask(w => w.Remove(this));
            else if (info.DistortionAnimation != 0)
            {
                for (var i = 1; i < offsets.Length - 1; i++)
                {
                    offsets[i] = new int2(
                        offsets[i].X + Game.CosmeticRandom.Next(-info.DistortionAnimation / 2, info.DistortionAnimation / 2),
                        offsets[i].Y + Game.CosmeticRandom.Next(-info.DistortionAnimation / 2, info.DistortionAnimation / 2)
                    );
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

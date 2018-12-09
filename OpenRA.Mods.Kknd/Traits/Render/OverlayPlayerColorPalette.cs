using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Render
{
	[Desc("More beautiful variant of the PlayerColorPalette by using the overlay blend mode.")]
	public class OverlayPlayerColorPaletteInfo : ITraitInfo
	{
		[Desc("The name of the palette to base off.")]
		[PaletteReference] public readonly string BasePalette = null;

		[Desc("The prefix for the resulting player palettes")]
		[PaletteDefinition(true)] public readonly string BaseName = "player";

		[Desc("Remap these indices to player colors.")]
		public readonly int[] RemapIndex = { };

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new OverlayPlayerColorPalette(this); }
	}

	public class OverlayPlayerColorPalette : ILoadsPlayerPalettes
	{
		private readonly OverlayPlayerColorPaletteInfo info;

		public OverlayPlayerColorPalette(OverlayPlayerColorPaletteInfo info)
		{
			this.info = info;
		}

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, HSLColor c, bool replaceExisting)
		{
			var pal = new MutablePalette(wr.Palette(info.BasePalette).Palette);

			foreach (var i in info.RemapIndex)
			{
				var bw = (float)(((pal[i] & 0xff) + ((pal[i] >> 8) & 0xff) + ((pal[i] >> 16) & 0xff)) / 3) / 0xff;
				var dstR = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)c.RGB.R / 0xff) : 2 * bw * ((float)c.RGB.R / 0xff);
				var dstG = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)c.RGB.G / 0xff) : 2 * bw * ((float)c.RGB.G / 0xff);
				var dstB = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)c.RGB.B / 0xff) : 2 * bw * ((float)c.RGB.B / 0xff);
				pal[i] = (pal[i] & 0xff000000) | ((uint)(dstR * 0xff) << 16) | ((uint)(dstG * 0xff) << 8) | (uint)(dstB * 0xff);
			}

			wr.AddPalette(info.BaseName + playerName, new ImmutablePalette(pal), info.AllowModifiers, replaceExisting);
		}
	}
}

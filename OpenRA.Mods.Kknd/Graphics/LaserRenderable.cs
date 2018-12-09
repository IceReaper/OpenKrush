using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.Kknd.Graphics
{
	public struct LaserRenderable : IRenderable, IFinalizedRenderable
	{
		readonly int2[] offsets;
		readonly int zOffset;
		readonly WDist width;
		readonly Color color;

		public LaserRenderable(int2[] offsets, int zOffset, WDist width, Color color)
		{
			this.offsets = offsets;
			this.zOffset = zOffset;
			this.width = width;
			this.color = color;
		}

		public WPos Pos { get { return new WPos(offsets[0].X, offsets[0].Y, 0); } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return new LaserRenderable(offsets, newOffset, width, color); }
		public IRenderable OffsetBy(WVec vec) { return this; } // TODO this one is wrong
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var screenWidth = wr.ScreenVector(new WVec(width, WDist.Zero, WDist.Zero))[0];
			Game.Renderer.WorldRgbaColorRenderer.DrawLine(offsets.Select(offset => wr.Screen3DPosition(new WPos(offset.X, offset.Y, 0))), screenWidth, color, false); // TODO fix bool
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}

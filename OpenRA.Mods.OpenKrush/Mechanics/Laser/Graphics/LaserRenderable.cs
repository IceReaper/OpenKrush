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

namespace OpenRA.Mods.OpenKrush.Mechanics.Laser.Graphics;

using OpenRA.Graphics;
using Primitives;

public readonly struct LaserRenderable : IRenderable, IFinalizedRenderable
{
	private readonly WPos[] offsets;
	private readonly WDist width;
	private readonly Color color;

	public LaserRenderable(WPos[] offsets, int zOffset, WDist width, Color color)
	{
		this.offsets = offsets;
		this.ZOffset = zOffset;
		this.width = width;
		this.color = color;
	}

	public WPos Pos => new(this.offsets[0].X, this.offsets[0].Y, 0);
	public int ZOffset { get; }
	public bool IsDecoration => true;

	public IRenderable WithZOffset(int newOffset)
	{
		return new LaserRenderable(this.offsets, newOffset, this.width, this.color);
	}

	public IRenderable OffsetBy(in WVec vec)
	{
		var vecCopy = vec;

		return new LaserRenderable(this.offsets.Select(offset => offset + vecCopy).ToArray(), this.ZOffset, this.width, this.color);
	}

	public IRenderable AsDecoration()
	{
		return this;
	}

	public IFinalizedRenderable PrepareRender(WorldRenderer wr)
	{
		return this;
	}

	public void Render(WorldRenderer wr)
	{
		// TODO fix connectSegments - asin smoothen the edge of a break!
		var screenWidth = wr.ScreenVector(new(this.width, WDist.Zero, WDist.Zero))[0];
		Game.Renderer.WorldRgbaColorRenderer.DrawLine(this.offsets.Select(wr.Screen3DPosition), screenWidth, this.color);
	}

	public void RenderDebugGeometry(WorldRenderer wr)
	{
	}

	public Rectangle ScreenBounds(WorldRenderer wr)
	{
		return Rectangle.Empty;
	}
}

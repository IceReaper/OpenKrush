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

namespace OpenRA.Mods.OpenKrush.Mechanics.Ui.Traits;

using System.Collections.Generic;
using Common.Traits;
using JetBrains.Annotations;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[TraitLocation(SystemActors.World)]
[Desc("Draws the planned path of local player units for a short time after a Move order is issued.")]
public class MovePathDisplayInfo : TraitInfo
{
	[Desc("Duration in ticks for which the path is displayed.")]
	public readonly int DisplayTime = 50;

	[Desc("Line color of the displayed path.")]
	public readonly Color LineColor = Color.Yellow;

	[Desc("Line width in pixels.")]
	public readonly int LineWidth = 3;

	public override object Create(ActorInitializer init)
	{
		return new MovePathDisplay(this);
	}
}

sealed class PathLine
{
	public readonly Actor Actor;
	public readonly List<CPos> Cells;
	public readonly int ExpireTick;

	public PathLine(Actor actor, List<CPos> cells, int expireTick)
	{
		Actor = actor;
		Cells = cells;
		ExpireTick = expireTick;
	}

	public IEnumerable<WPos> Waypoints(World world)
	{
		var pos = Actor.CenterPosition;
		var start = 0;
		var bestDist = long.MaxValue;
		for (var i = 0; i < Cells.Count; i++)
		{
			var c = world.Map.CenterOfSubCell(Cells[i], SubCell.FullCell);
			var dx = (long)c.X - pos.X;
			var dy = (long)c.Y - pos.Y;
			var d = dx * dx + dy * dy;
			if (d < bestDist)
			{
				bestDist = d;
				start = i;
			}
		}

		yield return pos;
		for (var i = start; i < Cells.Count; i++)
			yield return world.Map.CenterOfSubCell(Cells[i], SubCell.FullCell);
	}
}

public class MovePathDisplay : ITick, IValidateOrder, IRenderAnnotations
{
	readonly MovePathDisplayInfo info;
	readonly List<PathLine> lines = new();

	public MovePathDisplay(MovePathDisplayInfo info)
	{
		this.info = info;
	}

	bool IValidateOrder.OrderValidation(OrderManager orderManager, World world, int clientId, Order order)
	{
		if (order.OrderString != "Move" || order.Subject == null || order.Subject.IsDead || order.Subject.Owner != world.LocalPlayer)
			return true;

		var mobile = order.Subject.TraitOrDefault<Mobile>();
		if (mobile == null || mobile.IsTraitDisabled || order.Target.Type == TargetType.Invalid)
			return true;

		var targetCell = world.Map.Clamp(world.Map.CellContaining(order.Target.CenterPosition));
		if (!mobile.Info.LocomotorInfo.MoveIntoShroud && !world.LocalPlayer.Shroud.IsExplored(targetCell))
			return true;

		var path = mobile.PathFinder.FindPathToTargetCell(
			order.Subject,
			new[] { mobile.ToCell },
			mobile.NearestMoveableCell(targetCell),
			BlockedByActor.All,
			laneBias: true);

		if (path.Count == 0)
			return true;

		path.Reverse();

		if (mobile.FromCell != mobile.ToCell)
			path.Insert(0, mobile.FromCell);

		lines.RemoveAll(l => l.Actor == order.Subject);
		lines.Add(new PathLine(order.Subject, path, world.WorldTick + info.DisplayTime));

		return true;
	}

	void ITick.Tick(Actor self)
	{
		lines.RemoveAll(l => l.ExpireTick <= self.World.WorldTick || !l.Actor.IsInWorld || l.Actor.IsDead);
	}

	IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
	{
		if (lines.Count == 0)
			yield break;

		var now = wr.World.WorldTick;
		var fadeTicks = System.Math.Max(1, info.DisplayTime / 4);
		foreach (var line in lines)
		{
			var remaining = line.ExpireTick - now;
			if (remaining <= 0 || !line.Actor.IsInWorld || line.Actor.IsDead)
				continue;

			var alpha = 255f * System.Math.Min(1f, remaining / (float)fadeTicks);
			var color = Color.FromArgb((int)alpha, info.LineColor);
			yield return new TargetLineRenderable(line.Waypoints(wr.World), color, info.LineWidth, 2);
		}
	}

	bool IRenderAnnotations.SpatiallyPartitionable => false;
}

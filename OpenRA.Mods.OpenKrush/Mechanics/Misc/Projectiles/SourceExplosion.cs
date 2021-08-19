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

namespace OpenRA.Mods.OpenKrush.Mechanics.Misc.Projectiles
{
	using GameRules;
	using Graphics;
	using JetBrains.Annotations;
	using OpenRA.Traits;
	using System;
	using System.Collections.Generic;

	[UsedImplicitly]
	[Desc("Explodes on the attacker position instead of the target position.")]
	public class SourceExplosionInfo : IProjectileInfo
	{
		public IProjectile Create(ProjectileArgs args)
		{
			return new SourceExplosion(args);
		}
	}

	public class SourceExplosion : IProjectile
	{
		public SourceExplosion(ProjectileArgs args)
		{
			args.Weapon.Impact(Target.FromPos(args.SourceActor.CenterPosition), args.SourceActor);
		}

		public void Tick(World world)
		{
		}

		public IEnumerable<IRenderable> Render(WorldRenderer r)
		{
			return Array.Empty<IRenderable>();
		}
	}
}

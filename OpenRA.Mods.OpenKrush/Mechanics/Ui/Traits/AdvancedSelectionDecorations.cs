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

namespace OpenRA.Mods.OpenKrush.Mechanics.Ui.Traits
{
	using Common.Traits;
	using Graphics;
	using JetBrains.Annotations;
	using OpenRA.Graphics;
	using OpenRA.Traits;
	using Primitives;
	using System;
	using System.Collections.Generic;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[Desc("Healthbar box.")]
	public class AdvancedSelectionDecorationsInfo : TraitInfo
	{
		[Desc("Width for the decoration bar.")]
		public readonly int Width = 32;

		[Desc("Use big variant?")]
		public readonly bool BigVariant;

		[Desc("Offset for the decoration bar.")]
		public readonly int2 Offset = int2.Zero;

		public override object Create(ActorInitializer init)
		{
			return new AdvancedSelectionDecorations(this);
		}
	}

	public class AdvancedSelectionDecorations : ISelectionDecorations, IRenderAnnotations, INotifyCreated, INotifyOwnerChanged
	{
		private readonly AdvancedSelectionDecorationsInfo info;
		private StatusBar? statusBar;
		private Health? health;

		public AdvancedSelectionDecorations(AdvancedSelectionDecorationsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			this.health = self.TraitOrDefault<Health>();
		}

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer wr)
		{
			this.statusBar ??= new(self, this.info);

			if (!self.CanBeViewedByPlayer(self.World.LocalPlayer))
				yield break;

			if (!self.World.Selection.Contains(self))
			{
				switch (Game.Settings.Game.StatusBars)
				{
					case StatusBarsType.Standard:
						yield break;

					case StatusBarsType.DamageShow:
						if (this.health == null || this.health.DamageState == DamageState.Undamaged)
							yield break;

						break;

					case StatusBarsType.AlwaysShow:
						break;

					default:
						throw new ArgumentOutOfRangeException(Enum.GetName(Game.Settings.Game.StatusBars));
				}
			}

			yield return this.statusBar;
		}

		public IEnumerable<IRenderable> RenderSelectionAnnotations(Actor self, WorldRenderer worldRenderer, Color color)
		{
			yield break;
		}

		public int2 GetDecorationOrigin(Actor self, WorldRenderer wr, string pos, int2 margin)
		{
			return int2.Zero;
		}

		bool IRenderAnnotations.SpatiallyPartitionable => true;

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			this.statusBar = new(self, this.info);
		}
	}
}

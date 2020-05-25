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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Render
{
	[Desc("Add KKnD style healthbar box.")]
	public class AdvancedSelectionDecorationsInfo : ITraitInfo
	{
		[Desc("Width for the decoration bar.")]
		public readonly int Width = 32;

		[Desc("Use big variant?")]
		public readonly bool BigVariant = false;

		[Desc("Offset for the decoration bar.")]
		public readonly int2 Offset = int2.Zero;

		public object Create(ActorInitializer init) { return new AdvancedSelectionDecorations(this); }
	}

	public class AdvancedSelectionDecorations : ISelectionDecorations, IRenderAnnotations, INotifyCreated, INotifyOwnerChanged
	{
		private AdvancedSelectionDecorationsInfo info;
		private StatusBar statusBar;
		private Health health;

		public AdvancedSelectionDecorations(AdvancedSelectionDecorationsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			health = self.TraitOrDefault<Health>();
		}

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (statusBar == null)
				statusBar = new StatusBar(self, info);

			if (!self.CanBeViewedByPlayer(self.World.LocalPlayer))
				yield break;

			if (!self.World.Selection.Contains(self))
			{
				switch (Game.Settings.Game.StatusBars)
				{
					case StatusBarsType.Standard:
						yield break;

					case StatusBarsType.DamageShow:
						if (health == null || health.DamageState == DamageState.Undamaged)
							yield break;
						break;

					case StatusBarsType.AlwaysShow:
						break;
				}
			}

			yield return statusBar;
		}

		void ISelectionDecorations.DrawRollover(Actor self, WorldRenderer worldRenderer)
		{
			if (statusBar == null)
				statusBar = new StatusBar(self, info);

			statusBar.Render(worldRenderer);
		}

		bool IRenderAnnotations.SpatiallyPartitionable { get { return true; } }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			statusBar = new StatusBar(self, info);
		}
	}
}

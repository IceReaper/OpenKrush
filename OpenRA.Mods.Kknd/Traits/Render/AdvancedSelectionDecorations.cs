#region Copyright & License Information
/*
 * Copyright 2007-2021 The KKnD Developers (see AUTHORS)
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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Render
{
	[Desc("Add KKnD style healthbar box.")]
	public class AdvancedSelectionDecorationsInfo : TraitInfo
	{
		[Desc("Width for the decoration bar.")]
		public readonly int Width = 32;

		[Desc("Use big variant?")]
		public readonly bool BigVariant = false;

		[Desc("Offset for the decoration bar.")]
		public readonly int2 Offset = int2.Zero;

		public override object Create(ActorInitializer init) { return new AdvancedSelectionDecorations(this); }
	}

	public class AdvancedSelectionDecorations : ISelectionDecorations, IRenderAnnotations, INotifyCreated, INotifyOwnerChanged
	{
		public AdvancedSelectionDecorationsInfo Info;
		private StatusBar statusBar;
		private Health health;

		public AdvancedSelectionDecorations(AdvancedSelectionDecorationsInfo info)
		{
			Info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			health = self.TraitOrDefault<Health>();
		}

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (statusBar == null)
				statusBar = new StatusBar(self, Info);

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

		public IEnumerable<IRenderable> RenderSelectionAnnotations(Actor self, WorldRenderer worldRenderer, Color color)
		{
			yield break;
		}

		public int2 GetDecorationOrigin(Actor self, WorldRenderer wr, string pos, int2 margin)
		{
			return GetDecorationOrigin(self, wr, pos, margin);
		}

		bool IRenderAnnotations.SpatiallyPartitionable { get { return true; } }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			statusBar = new StatusBar(self, Info);
		}
	}
}

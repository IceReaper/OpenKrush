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

using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets.GameSelect
{
	public class GameSelectWidget : Widget
	{
		public string Game;
		public int State;

		public GameSelectWidget()
		{
			Bounds = new Rectangle((OpenRA.Game.Renderer.Resolution.Width - 1024) / 2, (OpenRA.Game.Renderer.Resolution.Height - 830) / 2, 1024, 830);
			AddChild(new GameButtonWidget("kknd1", 0, this));
			AddChild(new GameButtonWidget("kknd2", 1, this));
		}

		public override void Tick()
		{
			if (Game != null)
				OpenRA.Game.RunAfterTick(() => OpenRA.Game.InitializeMod(Game, Arguments.Empty));
		}
	}
}

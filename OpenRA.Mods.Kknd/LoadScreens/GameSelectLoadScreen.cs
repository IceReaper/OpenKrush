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
using OpenRA.Mods.Kknd.Widgets.GameSelect;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.LoadScreens
{
	public class GameSelectLoadScreen : ILoadScreen
	{
		public void Dispose()
		{
		}

		public void Init(ModData m, Dictionary<string, string> info)
		{
		}

		public void Display()
		{
		}

		public bool BeforeLoad()
		{
			return true;
		}

		public void StartGame(Arguments args)
		{
			Ui.Root.AddChild(new GameSelectWidget());
		}
	}
}

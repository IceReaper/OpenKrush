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

namespace OpenRA.Mods.OpenKrush.Traits.Production
{
	using Common.Traits;
	using OpenRA.Traits;
	using OpenRA.Widgets;
	using Widgets.Ingame;

	public class FocusInUiInfo : ConditionalTraitInfo
	{
		[FieldLoader.RequireAttribute]
		public readonly string Category = null;

		public override object Create(ActorInitializer init)
		{
			return new FocusInUi(this);
		}
	}

	public class FocusInUi : ConditionalTrait<FocusInUiInfo>, INotifySelected
	{
		public FocusInUi(FocusInUiInfo info)
			: base(info)
		{
		}

		void INotifySelected.Selected(Actor self)
		{
			if (self.Owner != self.World.LocalPlayer || IsTraitDisabled)
				return;

			var sidebar = Ui.Root.GetOrNull<SidebarWidget>(SidebarWidget.Identifier);

			if (sidebar == null)
				return;

			sidebar.SelectFactory(self, Info.Category);
		}
	}
}

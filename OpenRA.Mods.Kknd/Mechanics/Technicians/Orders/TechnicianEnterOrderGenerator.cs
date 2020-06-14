#region Copyright & License Information

/*
 * Copyright 2016-2020 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Kknd.Mechanics.Technicians.Traits;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Technicians.Orders
{
	public class TechnicianEnterOrderGenerator : UnitOrderGenerator
	{
		private IEnumerable<Actor> technicians = new List<Actor>();

		public override void Tick(World world)
		{
			technicians = world.ActorsHavingTrait<Technician>().Where(e => e.Owner == world.LocalPlayer && e.IsIdle);

			if (!technicians.Any())
				world.CancelInputMode();
		}

		public override IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
				world.CancelInputMode();
			else
			{
				var technician = technicians.OrderBy(e => (e.CenterPosition - world.Map.CenterOfCell(cell)).Length).First();
				var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a => TechnicianUtils.CanEnter(technician, a));

				if (actor == null)
					yield break;

				yield return new Order(TechnicianEnterOrderTargeter.Id, technician, Target.FromActor(actor), true);
			}
		}

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var technician = technicians.OrderBy(e => (e.CenterPosition - world.Map.CenterOfCell(cell)).Length).FirstOrDefault();

			if (technician == null)
				return null;

			var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a => TechnicianUtils.CanEnter(technician, a));
			var info = technician.Info.TraitInfo<TechnicianInfo>();

			return actor != null ? info.Cursor : info.BlockedCursor;
		}

		public override bool InputOverridesSelection(World world, int2 xy, MouseInput mi)
		{
			return true;
		}
	}
}

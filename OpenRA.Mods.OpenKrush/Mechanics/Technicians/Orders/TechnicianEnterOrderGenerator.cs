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

namespace OpenRA.Mods.OpenKrush.Mechanics.Technicians.Orders
{
	using OpenRA.Orders;
	using OpenRA.Traits;
	using System.Collections.Generic;
	using System.Linq;
	using Traits;

	public class TechnicianEnterOrderGenerator : UnitOrderGenerator
	{
		private IEnumerable<Actor> technicians = new List<Actor>();

		public override void Tick(World world)
		{
			this.technicians = world.ActorsHavingTrait<Technician>().Where(e => e.Owner == world.LocalPlayer && e.IsIdle);

			if (!this.technicians.Any())
				world.CancelInputMode();
		}

		public override IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
				world.CancelInputMode();
			else
			{
				var technician = this.technicians.OrderBy(e => (e.CenterPosition - world.Map.CenterOfCell(cell)).Length).FirstOrDefault();

				if (technician == null)
					yield break;

				var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a => TechnicianUtils.CanEnter(technician, a));

				if (actor == null)
					yield break;

				yield return new(TechnicianEnterOrderTargeter.Id, technician, Target.FromActor(actor), true);
			}
		}

		public override string? GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var technician = this.technicians.OrderBy(e => (e.CenterPosition - world.Map.CenterOfCell(cell)).Length).FirstOrDefault();

			if (technician == null)
				return null;

			var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a => TechnicianUtils.CanEnter(technician, a));
			var info = technician.Info.TraitInfoOrDefault<TechnicianInfo>();

			return actor != null ? info.Cursor : info.BlockedCursor;
		}

		public override bool InputOverridesSelection(World world, int2 xy, MouseInput mi)
		{
			return true;
		}
	}
}

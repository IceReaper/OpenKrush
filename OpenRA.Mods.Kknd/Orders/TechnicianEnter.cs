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
using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Kknd.Traits.Technicians;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Orders
{
	public class TechnicianEnterOrderGenerator : UnitOrderGenerator
	{
		private IEnumerable<TraitPair<Technician>> technicians;

		public new IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
				world.CancelInputMode();
			else
			{
				var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a =>
				{
					if (a.Owner != world.LocalPlayer)
						return false;

					var technicianRepairable = a.TraitOrDefault<TechnicianRepairable>();
					return technicianRepairable != null && !technicianRepairable.IsTraitDisabled;
				});

				if (actor != null)
				{
					var researches = technicians.OrderBy(e => (e.Actor.CenterPosition - actor.CenterPosition).Length).First().Actor;
					yield return new Order(TechnicianEnterOrderTargeter.Id, researches, Target.FromActor(actor), false);
				}
			}
		}

		public new void Tick(World world)
		{
			technicians = world.ActorsWithTrait<Technician>().Where(e => e.Actor.Owner == world.LocalPlayer && e.Actor.IsIdle);

			if (!technicians.Any())
				world.CancelInputMode();
		}

		public new string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a =>
			{
				if (a.Owner != world.LocalPlayer)
					return false;

				var technicianRepairable = a.TraitOrDefault<TechnicianRepairable>();
				return technicianRepairable != null && !technicianRepairable.IsTraitDisabled;
			});
			return actor != null ? "repair" : null;
		}
	}

	class TechnicianEnterOrderTargeter : UnitOrderTargeter
	{
		public const string Id = "TechnicianEnter";

		private string cursor;
		private string blockedCursor;

		public TechnicianEnterOrderTargeter(string cursor, string blockedCursor)
			: base(Id, 6, cursor, false, true)
		{
			this.cursor = cursor;
			this.blockedCursor = blockedCursor;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			cursor = null;

			if (!target.Info.HasTraitInfo<TechnicianRepairableInfo>())
				return false;

			if (target.Trait<TechnicianRepairable>().IsTraitDisabled)
			{
				cursor = blockedCursor;
				return false;
			}

			if (self.Owner.Stances[target.Owner] != Stance.Ally)
			{
				cursor = blockedCursor;
				return false;
			}

			cursor = this.cursor;
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}
	}
}

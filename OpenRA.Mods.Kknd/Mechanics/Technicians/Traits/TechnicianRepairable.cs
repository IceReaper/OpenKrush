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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Technicians.Traits
{
	[Desc("KKnD Technician mechanism, attach to the building.")]
	public class TechnicianRepairableInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("How many ticks the repair job will take.")]
		public readonly int Duration = 300;

		[Desc("How many HP does the repair job repair.")]
		public readonly int Amount = 3000;

		[Desc("Offset for the repair sequence.")]
		public readonly int2 Offset = int2.Zero;

		public override object Create(ActorInitializer init)
		{
			return new TechnicianRepairable(init, this);
		}
	}

	public class RepairTask
	{
		public int Duration;
		public int Amount;

		public RepairTask(int amount, int duration)
		{
			Amount = amount;
			Duration = duration;
		}
	}

	public class TechnicianRepairable : ConditionalTrait<TechnicianRepairableInfo>, ITick
	{
		private readonly List<RepairTask> repairTasks = new List<RepairTask>();

		public TechnicianRepairable(ActorInitializer init, TechnicianRepairableInfo info)
			: base(info)
		{
			var rs = init.Self.Trait<RenderSprites>();
			var body = init.Self.Trait<BodyOrientation>();

			var overlay = new Animation(init.World, "indicators", () => repairTasks.Count == 0);
			overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, init.Self.GetDamageState(), "repair"));

			// TODO body.LocalToWorld messes up the position.
			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(
					new WVec(info.Offset.Y * -32, info.Offset.X * -32, 0).Rotate(body.QuantizeOrientation(init.Self, init.Self.Orientation))),
				() => repairTasks.Count == 0,
				p => RenderUtils.ZOffsetFromCenter(init.Self, p, 1));

			rs.Add(anim);
		}

		void ITick.Tick(Actor self)
		{
			foreach (var repairTask in repairTasks.ToList())
			{
				var amount = repairTask.Amount / repairTask.Duration;
				repairTask.Amount -= amount;
				repairTask.Duration -= 1;
				self.InflictDamage(self, new Damage(-amount));

				if (repairTask.Duration == 0)
					repairTasks.Remove(repairTask);
			}
		}

		public void Enter()
		{
			repairTasks.Add(new RepairTask(Info.Amount, Info.Duration));
		}
	}
}

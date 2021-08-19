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

namespace OpenRA.Mods.OpenKrush.Mechanics.Technicians.Traits
{
	using Common.Traits;
	using Common.Traits.Render;
	using Graphics;
	using JetBrains.Annotations;
	using OpenRA.Traits;
	using System.Collections.Generic;
	using System.Linq;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[Desc("Technician mechanism, attach to the building.")]
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
			this.Amount = amount;
			this.Duration = duration;
		}
	}

	public class TechnicianRepairable : ConditionalTrait<TechnicianRepairableInfo>, ITick
	{
		private readonly List<RepairTask> repairTasks = new();

		public TechnicianRepairable(ActorInitializer init, TechnicianRepairableInfo info)
			: base(info)
		{
			var rs = init.Self.TraitOrDefault<RenderSprites>();
			var body = init.Self.TraitOrDefault<BodyOrientation>();

			var overlay = new Animation(init.World, "indicators", () => this.repairTasks.Count == 0);
			overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, init.Self.GetDamageState(), "repair"));

			// TODO body.LocalToWorld messes up the position.
			var anim = new AnimationWithOffset(
				overlay,
				() => body.LocalToWorld(
					new WVec(info.Offset.Y * -32, info.Offset.X * -32, 0).Rotate(body.QuantizeOrientation(init.Self, init.Self.Orientation))
				),
				() => this.repairTasks.Count == 0,
				p => RenderUtils.ZOffsetFromCenter(init.Self, p, 1)
			);

			rs.Add(anim);
		}

		void ITick.Tick(Actor self)
		{
			foreach (var repairTask in this.repairTasks.ToList())
			{
				var amount = repairTask.Amount / repairTask.Duration;
				repairTask.Amount -= amount;
				repairTask.Duration -= 1;
				self.InflictDamage(self, new(-amount));

				if (repairTask.Duration == 0)
					this.repairTasks.Remove(repairTask);
			}
		}

		public void Enter()
		{
			this.repairTasks.Add(new(this.Info.Amount, this.Info.Duration));
		}
	}
}

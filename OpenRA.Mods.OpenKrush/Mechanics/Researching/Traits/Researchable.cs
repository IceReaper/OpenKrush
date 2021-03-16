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

namespace OpenRA.Mods.OpenKrush.Mechanics.Researching.Traits
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Common.Traits;
	using Common.Traits.Render;
	using LobbyOptions;
	using OpenRA.Graphics;
	using OpenRA.Traits;

	[Desc("Research mechanism, attach to the actor which has tech levels.")]
	public class ResearchableInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Research sequence name to use.")]
		public readonly string Sequence = "research";

		[Desc("Base duration of research.")]
		public readonly int ResearchTimeBase = 400;

		[Desc("Additional duration of research per tech level.")]
		public readonly int ResearchTimeTechLevel = 300;

		[Desc("Base cost of research.")]
		public readonly int ResearchCostBase = 250;

		[Desc("Additional cost of research per tech level.")]
		public readonly int ResearchCostTechLevel = 500;

		[Desc("Offset for the research sequence.")]
		public readonly int2 Offset = int2.Zero;

		public override object Create(ActorInitializer init)
		{
			return new Researchable(init, this);
		}
	}

	public class Researchable : ConditionalTrait<ResearchableInfo>, INotifyAddedToWorld
	{
		private class Technology
		{
			public readonly string Id;
			public readonly int Level;
			public bool Researched;

			public Technology(string id, int level)
			{
				Id = id;
				Level = level;
			}
		}

		private readonly ResearchableInfo info;
		private readonly Actor self;

		private readonly Animation overlay;
		private readonly int researchSteps;

		private Technology[] technologies;
		public int Level;
		public int MaxLevel;
		public int LimitedLevels;
		public Researches ResearchedBy;

		public Researchable(ActorInitializer init, ResearchableInfo info)
			: base(info)
		{
			this.info = info;
			self = init.Self;

			var renderSprites = self.Trait<RenderSprites>();
			var bodyOrientation = self.Trait<BodyOrientation>();

			var hidden = new Func<bool>(() => ResearchedBy == null || !self.Owner.IsAlliedWith(self.World.LocalPlayer));

			overlay = new Animation(self.World, "indicators", hidden);
			overlay.PlayRepeating(info.Sequence + 0);

			var anim = new AnimationWithOffset(
				overlay,
				() => bodyOrientation.LocalToWorld(
					new WVec(info.Offset.Y * -32, info.Offset.X * -32, 0).Rotate(bodyOrientation.QuantizeOrientation(self, self.Orientation))),
				hidden,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			renderSprites.Add(anim);

			while (overlay.HasSequence(info.Sequence + researchSteps))
				researchSteps++;
		}

		public void SetProgress(float progress)
		{
			var sequence = info.Sequence + (int)Math.Floor(researchSteps * progress);

			if (overlay.CurrentSequence.Name != sequence)
				overlay.PlayRepeating(sequence);
		}

		public ResarchState GetState()
		{
			if (IsTraitDisabled)
				return ResarchState.Unavailable;

			if (self.IsDead || !self.IsInWorld)
				return ResarchState.Unavailable;

			if (ResearchedBy != null)
				return ResarchState.Researching;

			return ResarchState.Available;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var researchMode = self.World.WorldActor.Trait<ResearchMode>().Mode;
			var techLimit = self.World.WorldActor.Trait<TechLimit>().Limit;

			var techTrees = self.TraitsImplementing<IProvidesResearchables>();

			var allTechnologies = new List<Technology>();

			foreach (var techTree in techTrees)
				allTechnologies.AddRange(
					techTree.GetResearchables()
						.Where(
							entry =>
							{
								if (entry.Value <= techLimit)
									return entry.Value >= 0;

								if (researchMode == ResearchModeType.FullLevel)
									LimitedLevels = Math.Max(entry.Value - techLimit, LimitedLevels);
								else
									LimitedLevels++;

								return false;
							})
						.Select(entry => new Technology(entry.Key, entry.Value)));

			if (!allTechnologies.Any())
				return;

			technologies = allTechnologies.OrderBy(technology => technology.Level).ToArray();

			Level = technologies[0].Level;

			foreach (var technology in technologies.Where(technology => technology.Level == Level))
				technology.Researched = true;

			if (researchMode == ResearchModeType.FullLevel)
				MaxLevel = technologies[technologies.Length - 1].Level;
			else
			{
				Level = 0;
				MaxLevel = technologies.Count(technology => !technology.Researched);
			}
		}

		public void Researched()
		{
			if (Level == MaxLevel)
				return;

			Level++;

			var researchMode = self.World.WorldActor.Trait<ResearchMode>().Mode;

			if (researchMode == ResearchModeType.SingleTech)
				technologies.First(technology => technology.Researched == false).Researched = true;
			else
				foreach (var technology in technologies.Where(technology => technology.Level == Level))
					technology.Researched = true;
		}

		public bool IsResearched(string id)
		{
			var technology = technologies.FirstOrDefault(t => t.Id == id);

			return technology != null && technology.Researched;
		}

		public int NextTechLevel()
		{
			return technologies.FirstOrDefault(technology => !technology.Researched)?.Level ?? 0;
		}
	}
}

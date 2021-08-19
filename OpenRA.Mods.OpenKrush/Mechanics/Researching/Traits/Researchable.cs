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
	using Common.Traits;
	using Common.Traits.Render;
	using Graphics;
	using JetBrains.Annotations;
	using LobbyOptions;
	using OpenRA.Traits;
	using System;
	using System.Collections.Generic;
	using System.Linq;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
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
				this.Id = id;
				this.Level = level;
			}
		}

		private readonly ResearchableInfo info;

		private readonly Animation overlay;
		private readonly int researchSteps;

		private Technology[] technologies = Array.Empty<Technology>();
		public int Level;
		public int MaxLevel;
		public int LimitedLevels;
		public Actor? ResearchedBy;
		public Researches? ResearchedByResearches;

		public Researchable(ActorInitializer init, ResearchableInfo info)
			: base(info)
		{
			this.info = info;

			var renderSprites = init.Self.TraitOrDefault<RenderSprites>();
			var bodyOrientation = init.Self.TraitOrDefault<BodyOrientation>();

			var hidden = new Func<bool>(() => this.ResearchedBy == null || !init.Self.Owner.IsAlliedWith(init.Self.World.LocalPlayer));

			this.overlay = new(init.Self.World, "indicators", hidden);
			this.overlay.PlayRepeating(info.Sequence + 0);

			var anim = new AnimationWithOffset(
				this.overlay,
				() => bodyOrientation.LocalToWorld(
					new WVec(info.Offset.Y * -32, info.Offset.X * -32, 0).Rotate(bodyOrientation.QuantizeOrientation(init.Self, init.Self.Orientation))
				),
				hidden,
				p => RenderUtils.ZOffsetFromCenter(init.Self, p, 1)
			);

			renderSprites.Add(anim);

			while (this.overlay.HasSequence(info.Sequence + this.researchSteps))
				this.researchSteps++;
		}

		public void SetProgress(float progress)
		{
			var sequence = this.info.Sequence + (int)Math.Floor(this.researchSteps * progress);

			if (this.overlay.CurrentSequence.Name != sequence)
				this.overlay.PlayRepeating(sequence);
		}

		public ResarchState GetState(Actor self)
		{
			if (this.IsTraitDisabled)
				return ResarchState.Unavailable;

			if (self.IsDead || !self.IsInWorld)
				return ResarchState.Unavailable;

			return this.ResearchedBy != null ? ResarchState.Researching : ResarchState.Available;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var researchMode = self.World.WorldActor.TraitOrDefault<ResearchMode>().Mode;
			var techLimit = self.World.WorldActor.TraitOrDefault<TechLimit>().Limit;

			var techTrees = self.TraitsImplementing<IProvidesResearchables>();

			var allTechnologies = new List<Technology>();

			foreach (var techTree in techTrees)
			{
				allTechnologies.AddRange(
					techTree.GetResearchables(self)
						.Where(
							entry =>
							{
								var (_, value) = entry;

								if (value <= techLimit)
									return value >= 0;

								if (researchMode == ResearchModeType.FullLevel)
									this.LimitedLevels = Math.Max(value - techLimit, this.LimitedLevels);
								else
									this.LimitedLevels++;

								return false;
							}
						)
						.Select(entry => new Technology(entry.Key, entry.Value))
				);
			}

			if (!allTechnologies.Any())
				return;

			this.technologies = allTechnologies.OrderBy(technology => technology.Level).ToArray();

			this.Level = this.technologies[0].Level;

			foreach (var technology in this.technologies.Where(technology => technology.Level == this.Level))
				technology.Researched = true;

			if (researchMode == ResearchModeType.FullLevel)
				this.MaxLevel = this.technologies[^1].Level;
			else
			{
				this.Level = 0;
				this.MaxLevel = this.technologies.Count(technology => !technology.Researched);
			}
		}

		public void Researched(Actor self)
		{
			if (this.Level == this.MaxLevel)
				return;

			var researchMode = self.World.WorldActor.TraitOrDefault<ResearchMode>().Mode;

			if (researchMode == ResearchModeType.SingleTech)
			{
				this.Level++;
				var technology = this.technologies.FirstOrDefault(technology => technology.Researched == false);

				if (technology != null)
					technology.Researched = true;
			}
			else
			{
				this.Level = this.NextTechLevel();

				foreach (var technology in this.technologies.Where(technology => technology.Level == this.Level))
					technology.Researched = true;
			}
		}

		public bool IsResearched(string id)
		{
			var technology = this.technologies.FirstOrDefault(t => t.Id == id);

			return technology is { Researched: true };
		}

		public int NextTechLevel()
		{
			return this.technologies.FirstOrDefault(technology => !technology.Researched)?.Level ?? 0;
		}
	}
}

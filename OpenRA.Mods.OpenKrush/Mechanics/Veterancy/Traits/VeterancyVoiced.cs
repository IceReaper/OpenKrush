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

namespace OpenRA.Mods.OpenKrush.Mechanics.Veterancy.Traits
{
	using Common.Traits;
	using JetBrains.Annotations;
	using OpenRA.Traits;
	using System;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[Desc("This actor has a different voice for each veterancy level.")]
	public class VeterancyVoicedInfo : TraitInfo
	{
		[FieldLoader.RequireAttribute]
		[Desc("Which voice sets to use.")]
		[VoiceSetReference]
		public readonly string[] VoiceSets = Array.Empty<string>();

		[Desc("Multiply volume with this factor.")]
		public readonly float Volume = 1f;

		public override object Create(ActorInitializer init)
		{
			return new VeterancyVoiced(this);
		}
	}

	public class VeterancyVoiced : IVoiced, INotifyCreated
	{
		private readonly VeterancyVoicedInfo info;
		private Veterancy? veterancy;

		public VeterancyVoiced(VeterancyVoicedInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			this.veterancy = self.TraitOrDefault<Veterancy>();
		}

		string IVoiced.VoiceSet => this.info.VoiceSets[this.veterancy != null ? Math.Min(this.veterancy.Level, this.info.VoiceSets.Length - 1) : 0];

		bool IVoiced.PlayVoice(Actor self, string? phrase, string variant)
		{
			if (phrase == null)
				return false;

			var type = ((IVoiced)this).VoiceSet.ToLowerInvariant();
			var volume = this.info.Volume;

			return Game.Sound.PlayPredefined(SoundType.World, self.World.Map.Rules, null, self, type, phrase, variant, true, WPos.Zero, volume, true);
		}

		bool IVoiced.PlayVoiceLocal(Actor self, string? phrase, string variant, float volume)
		{
			if (phrase == null)
				return false;

			var type = ((IVoiced)this).VoiceSet.ToLowerInvariant();

			return Game.Sound.PlayPredefined(
				SoundType.World,
				self.World.Map.Rules,
				null,
				self,
				type,
				phrase,
				variant,
				false,
				self.CenterPosition,
				volume,
				true
			);
		}

		bool IVoiced.HasVoice(Actor self, string voice)
		{
			var voices = self.World.Map.Rules.Voices[((IVoiced)this).VoiceSet.ToLowerInvariant()];

			return voices != null && voices.Voices.ContainsKey(voice);
		}
	}
}

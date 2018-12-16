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

using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Altar
{
    [Desc("Allow sacrificeable units to enter and spawn a new actor..")]
    public class AltarInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>
    {
        [Desc("Voice to use when sacrificed.")]
        [VoiceReference] public readonly string Voice = "Sacrificed";

        [Desc("The amount of units required for a sacrifice.")]
        public readonly int Sacrifices = 5;

        [Desc("Duration of the summoning.")]
        public readonly int SummonDelay = 50;

        [FieldLoader.Require]
        [Desc("The unit which is granted upon sacrificing.")]
        public readonly string Summon = null;

        [Desc("Offset relative to the top-left cell of the building.")]
        public readonly CVec SpawnOffset = CVec.Zero;

        [Desc("Which direction the unit should face.")]
        public readonly int Facing = 0;

        // TODO this should be sacrifice
        [Desc("Sequence 1 to be played when secrifing enough units.")]
        [SequenceReference] public readonly string Sequence1 = "sacrifice1";

        // TODO this should be summon
        [Desc("Sequence 2 to be played when secrifing enough units.")]
        [SequenceReference] public readonly string Sequence2 = "sacrifice2";

        [Desc("Position relative to body")]
        public readonly WVec Offset = WVec.Zero;

        public override object Create(ActorInitializer init) { return new Altar(init.Self, this); }
    }

    public class Altar : ConditionalTrait<AltarInfo>, ITick
    {
        private int summonTicker;

        public int Population { get; private set; }

        public Altar(Actor self, AltarInfo info) : base(info)
        {
            var rs = self.Trait<RenderSprites>();
            var body = self.Trait<BodyOrientation>();

            var overlay1 = new Animation(self.World, rs.GetImage(self));
            var overlay2 = new Animation(self.World, rs.GetImage(self));
            overlay1.PlayRepeating(RenderSprites.NormalizeSequence(overlay1, self.GetDamageState(), Info.Sequence1));
            overlay2.PlayRepeating(RenderSprites.NormalizeSequence(overlay2, self.GetDamageState(), Info.Sequence2));

            var anim1 = new AnimationWithOffset(overlay1,
                () => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
                () => IsTraitDisabled || summonTicker == 0,
                p => RenderUtils.ZOffsetFromCenter(self, p, 1));

            var anim2 = new AnimationWithOffset(overlay2,
                () => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
                () => IsTraitDisabled || summonTicker == 0,
                p => RenderUtils.ZOffsetFromCenter(self, p, 1));

            rs.Add(anim1);
            rs.Add(anim2);
        }

        public void Enter(Actor sacrifice)
        {
            Population++;
            sacrifice.PlayVoice(Info.Voice);

            if (Population == Info.Sacrifices)
                summonTicker = Info.SummonDelay;
        }

        void ITick.Tick(Actor self)
        {
            if (summonTicker == 0 || --summonTicker > 0)
                return;

            var numSummons = Population / Info.Sacrifices;
            Population -= numSummons * Info.Sacrifices;

            self.World.AddFrameEndTask(w =>
            {
                for (var i = 0; i < numSummons; i++)
                {
                    w.CreateActor(Info.Summon, new TypeDictionary
                    {
                        new ParentActorInit(self),
                        new LocationInit(self.Location + Info.SpawnOffset),
                        new OwnerInit(self.Owner),
                        new FacingInit(Info.Facing)
                    });

                    // TODO move into world here!
                }
            });
        }
    }
}

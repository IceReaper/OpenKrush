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

using OpenRA.Mods.Kknd.FileFormats;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets.GameSelect
{
    // TODO merge modcontent, so we can install assets before running.
    public class GameSelectWidget : Widget
    {
        private VbcPlayerWidget player;

        public string Game;
        public int State;

        public GameSelectWidget()
        {
            Bounds = new Rectangle((OpenRA.Game.Renderer.Resolution.Width - 1024) / 2, (OpenRA.Game.Renderer.Resolution.Height - 830) / 2, 1024, 830);
            AddChild(new GameButtonWidget("kknd1", 0, this));
            AddChild(new GameButtonWidget("kknd2", 1, this));
            AddChild(player = new VbcPlayerWidget());
        }

        public override void Tick()
        {
            if (State == 1)
            {
                Children.RemoveAll(child => child is GameButtonWidget);
                PlayVideo("Melbourne House");
            }
            else if (State == 3)
            {
                PlayVideo("Intro");
            }
            else if (State == 5)
                OpenRA.Game.RunAfterTick(() => OpenRA.Game.InitializeMod(Game, Arguments.Empty));
        }

        private void PlayVideo(string video)
        {
            if (OpenRA.Game.ModData.ModFiles.Exists("content|" + Game + "/Movies/" + video + ".vbc"))
            {
                player.Video = new Vbc(OpenRA.Game.ModData.ModFiles.Open("content|" + Game + "/Movies/" + video + ".vbc"));
                player.Bounds = new Rectangle(
                    (OpenRA.Game.Renderer.Resolution.Width - player.Video.Size.X) / 2,
                    (OpenRA.Game.Renderer.Resolution.Height - player.Video.Size.Y) / 2,
                    player.Video.Size.X,
                    player.Video.Size.Y);

                player.Play(() =>
                {
                    player.Visible = false;
                    State++;
                });
                State++;
            }
            else
                State += 2;
        }
    }
}

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.OpenKrush.GameProviders
{
    public static class Dos
    {
        public static bool TryRegister(string path)
        {
            path = GameProvider.GetDirectory(path, "kknd");

            if (path == null)
                return false;

            var executable = GameProvider.GetFile(path, "kknd.exe");

            if (executable == null)
                return false;

            var release = CryptoUtil.SHA1Hash(File.OpenRead(executable));

            // Krush, Kill 'N' Destroy (Disc, English)
            if (release != "024e96860c504b462b24b9237d49bfe8de6eb8e0")
                return false;

            var levelsFolder = GameProvider.GetDirectory(path, "levels");
            var fmvFolder = GameProvider.GetDirectory(path, "fmv");

            if (levelsFolder == null || fmvFolder == null)
                return false;

            var graphicsFolder = GameProvider.GetDirectory(levelsFolder, "640");

            if (graphicsFolder == null)
                return false;

            // Required files.
            var files = new Dictionary<string, string>
            {
                { "sprites.lvl", graphicsFolder },
                { "surv.slv", levelsFolder },
                { "mute.slv", levelsFolder },
                { "surv1.son", levelsFolder },
                { "surv2.son", levelsFolder },
                { "surv3.son", levelsFolder },
                { "mute1.son", levelsFolder },
                { "mute2.son", levelsFolder },
                { "mute3.son", levelsFolder },
                { "mh_fmv.vbc", fmvFolder },
                { "intro.vbc", fmvFolder }
            }.ToDictionary(e => e.Key, e => GameProvider.GetFile(e.Value, e.Key));

            if (files.Values.Any(v => v == null))
                return false;

            GameProvider.Installation = path;
            GameProvider.Packages.Add(files["sprites.lvl"], "sprites.lvl");
            GameProvider.Packages.Add(files["mute.slv"], "mute.slv");
            GameProvider.Packages.Add(files["surv.slv"], "surv.slv");

            GameProvider.Music.Add("Survivors 1", files["surv1.son"]);
            GameProvider.Music.Add("Survivors 2", files["surv2.son"]);
            GameProvider.Music.Add("Survivors 3", files["surv3.son"]);
            GameProvider.Music.Add("Evolved 1", files["mute1.son"]);
            GameProvider.Music.Add("Evolved 2", files["mute2.son"]);
            GameProvider.Music.Add("Evolved 3", files["mute3.son"]);

            GameProvider.Movies.Add("mh.vbc", files["mh_fmv.vbc"]);
            GameProvider.Movies.Add("intro.vbc", files["intro.vbc"]);

            // Any other container for asset browser purpose
            GameProvider.Packages.Add(levelsFolder, null);
            GameProvider.Packages.Add(fmvFolder, null);

            foreach (var file in Directory.GetFiles(levelsFolder).Concat(Directory.GetFiles(graphicsFolder)).Where(f =>
                f.EndsWith(".slv", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".lvl", StringComparison.OrdinalIgnoreCase)))
            {
                if (!GameProvider.Packages.ContainsKey(file))
                    GameProvider.Packages.Add(file, Path.GetFileName(file).ToLower());
            }

            return true;
        }
    }
}

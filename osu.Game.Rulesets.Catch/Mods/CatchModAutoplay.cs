// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Mods;
using osu.Android;

namespace osu.Game.Rulesets.Catch.Mods
{
    public class CatchModAutoplay : ModAutoplay
    {
        public override Type[] IncompatibleMods => base.IncompatibleMods.Concat(new[] { typeof(CatchModMovingFast) }).ToArray();

        public override ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => new ModReplayData(new CatchAutoGenerator(beatmap).Generate(), new ModCreatedUser { Username = "osu!salad" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ModMenuAutoplayApplicator — генерирует авто-реплей без включения мода.
    //
    // КАК ПОДКЛЮЧИТЬ в DrawableCatchRuleset.cs:
    //
    //   protected override ReplayInputHandler? CreateReplayInputHandler(Replay replay)
    //   {
    //       // Обычный реплей (например из файла) — используем как есть
    //       if (replay != null)
    //           return new CatchFramedReplayInputHandler(replay);
    //
    //       // ModMenu AutoPlay — генерируем реплей без мода
    //       if (ModMenu.AutoPlayEnabled)
    //           return ModMenuAutoplayApplicator.CreateHandler(Beatmap.Value.Beatmap);
    //
    //       return null;
    //   }
    // ─────────────────────────────────────────────────────────────────────────
    public static class ModMenuAutoplayApplicator
    {
        /// <summary>
        /// Создаёт CatchFramedReplayInputHandler на основе авто-генератора реплеев.
        /// Вызывать только когда ModMenu.AutoPlayEnabled == true.
        /// </summary>
        public static CatchFramedReplayInputHandler CreateHandler(IBeatmap beatmap)
        {
            var replay = new CatchAutoGenerator(beatmap).Generate();
            return new CatchFramedReplayInputHandler(replay);
        }
    }
}

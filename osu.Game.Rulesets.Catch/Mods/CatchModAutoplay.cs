// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Mods;

#if ANDROID
using osu.Android;
#endif

namespace osu.Game.Rulesets.Catch.Mods
{
    /// <summary>
    /// AutoPlay мод для Catch.
    /// На Android активируется исключительно через ModMenu оверлей,
    /// а не через встроенное меню модов игры.
    /// </summary>
    public class CatchModAutoplay : ModAutoplay
    {
        public override Type[] IncompatibleMods =>
            base.IncompatibleMods.Concat(new[] { typeof(CatchModMovingFast) }).ToArray();

        /// <summary>
        /// Возвращает true только если оверлей разрешает AutoPlay.
        /// Если оверлей недоступен (не Android), ведёт себя как оригинальный мод.
        /// </summary>
        public bool IsActive
        {
            get
            {
#if ANDROID
                return ModMenu.AutoPlayEnabled;
#else
                return true; // стандартное поведение на десктопе
#endif
            }
        }

        public override ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
        {
#if ANDROID
            // Генерируем реплей только если оверлей разрешил AutoPlay
            if (!ModMenu.AutoPlayEnabled)
                return new ModReplayData(new EmptyReplayGenerator(beatmap).Generate(), new ModCreatedUser { Username = "osu!salad" });
#endif
            return new ModReplayData(
                new CatchAutoGenerator(beatmap).Generate(),
                new ModCreatedUser { Username = "osu!salad" }
            );
        }

#if ANDROID
        /// <summary>
        /// Пустой генератор реплея — используется когда AutoPlay выключен через оверлей.
        /// Возвращает реплей без фреймов, игрок управляет сам.
        /// </summary>
        private class EmptyReplayGenerator : global::osu.Game.Rulesets.Replays.AutoGenerator<CatchReplayFrame>
        {
            public EmptyReplayGenerator(IBeatmap beatmap) : base(beatmap) { }

            protected override void GenerateFrames()
            {
                // Намеренно пусто — игрок управляет вручную
            }
        }
#endif
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Mods;

#if ANDROID
using osu.Android;
#endif

namespace osu.Game.Rulesets.Catch.Mods
{
    /// <summary>
    /// NoFail (NoMiss) мод для Catch.
    /// На Android активируется исключительно через ModMenu оверлей.
    /// HealthProcessor проверяет <see cref="IsActive"/> перед тем как применять урон.
    /// </summary>
    public class CatchModNoFail : ModNoFail
    {
        /// <summary>
        /// Возвращает true только если оверлей разрешает NoMiss.
        /// На не-Android платформах всегда true (стандартное поведение мода).
        /// </summary>
        public bool IsActive
        {
            get
            {
#if ANDROID
                return ModMenu.NoMissEnabled;
#else
                return true;
#endif
            }
        }
    }
}

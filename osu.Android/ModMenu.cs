using System;
using System.Collections.Generic;

namespace osu.Android
{
    public static class ModMenu
    {
        public static bool AutoPlayEnabled { get; private set; }
        public static bool NoMissEnabled { get; private set; }
        public static bool RelaxEnabled { get; private set; }
        public static bool InstantSpinEnabled { get; private set; }

        public static void SetAutoPlay(bool enabled)
        {
            AutoPlayEnabled = enabled;

            if (enabled)
                NoMissEnabled = true;
        }

        public static void SetNoMiss(bool enabled) => NoMissEnabled = enabled;
        public static void SetRelax(bool enabled) => RelaxEnabled = enabled;
        public static void SetInstantSpin(bool enabled) => InstantSpinEnabled = enabled;

        public static Dictionary<string, bool> GetStates() => new()
        {
            ["AutoPlay"] = AutoPlayEnabled,
            ["NoMiss"] = NoMissEnabled,
            ["Relax"] = RelaxEnabled,
            ["InstantSpin"] = InstantSpinEnabled
        };
    }
}
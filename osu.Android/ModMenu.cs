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

        public static void ToggleAutoPlay()
        {
            AutoPlayEnabled = !AutoPlayEnabled;

            if (AutoPlayEnabled)
                NoMissEnabled = true;
        }

        public static void ToggleNoMiss() => NoMissEnabled = !NoMissEnabled;
        public static void ToggleRelax() => RelaxEnabled = !RelaxEnabled;
        public static void ToggleInstantSpin() => InstantSpinEnabled = !InstantSpinEnabled;

        public static Dictionary<string, bool> GetStates() => new()
        {
            ["AutoPlay"] = AutoPlayEnabled,
            ["NoMiss"] = NoMissEnabled,
            ["Relax"] = RelaxEnabled,
            ["InstantSpin"] = InstantSpinEnabled
        };
    }
}
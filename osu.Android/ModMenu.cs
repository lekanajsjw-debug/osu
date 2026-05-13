using System;

namespace osu.Android
{
    public static class ModMenu
    {
        public static bool AutoPlayEnabled { get; private set; }

        public static bool NoMissEnabled { get; private set; }

        public static bool RelaxEnabled { get; private set; }

        public static bool InstantSpinEnabled { get; private set; }

        public static bool ForceRankedEnabled { get; private set; }

        public static event Action? OnStateChanged;

        public static void ToggleAutoPlay()
        {
            AutoPlayEnabled = !AutoPlayEnabled;

            if (AutoPlayEnabled)
                NoMissEnabled = true;

            notify();
        }

        public static void ToggleNoMiss()
        {
            NoMissEnabled = !NoMissEnabled;
            notify();
        }

        public static void ToggleRelax()
        {
            RelaxEnabled = !RelaxEnabled;
            notify();
        }

        public static void ToggleInstantSpin()
        {
            InstantSpinEnabled = !InstantSpinEnabled;
            notify();
        }

        public static void ToggleForceRanked()
        {
            ForceRankedEnabled = !ForceRankedEnabled;
            notify();
        }

        private static void notify()
        {
            OnStateChanged?.Invoke();
        }
    }
}
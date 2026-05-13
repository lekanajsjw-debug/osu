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

        public static void SetAutoPlay(bool enabled)
        {
            AutoPlayEnabled = enabled;

            if (enabled)
                NoMissEnabled = true;

            notify();
        }

        public static void ToggleAutoPlay()
        {
            SetAutoPlay(!AutoPlayEnabled);
        }

        public static void SetNoMiss(bool enabled)
        {
            NoMissEnabled = enabled;
            notify();
        }

        public static void ToggleNoMiss()
        {
            SetNoMiss(!NoMissEnabled);
        }

        public static void SetRelax(bool enabled)
        {
            RelaxEnabled = enabled;
            notify();
        }

        public static void ToggleRelax()
        {
            SetRelax(!RelaxEnabled);
        }

        public static void SetInstantSpin(bool enabled)
        {
            InstantSpinEnabled = enabled;
            notify();
        }

        public static void ToggleInstantSpin()
        {
            SetInstantSpin(!InstantSpinEnabled);
        }

        public static void SetForceRanked(bool enabled)
        {
            ForceRankedEnabled = enabled;
            notify();
        }

        public static void ToggleForceRanked()
        {
            SetForceRanked(!ForceRankedEnabled);
        }

        private static void notify()
        {
            OnStateChanged?.Invoke();
        }
    }
}
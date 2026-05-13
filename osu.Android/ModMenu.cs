using System;

namespace osu.Android
{
    public static class ModMenu
    {
        public static bool RelaxEnabled;
        public static bool NoMissEnabled;
        public static bool HitboxEnabled;

        public static event Action? OnStateChanged;

        public static void SetRelax(bool value)
        {
            RelaxEnabled = value;
            OnStateChanged?.Invoke();
        }

        public static void SetNoMiss(bool value)
        {
            NoMissEnabled = value;
            OnStateChanged?.Invoke();
        }

        public static void SetHitbox(bool value)
        {
            HitboxEnabled = value;
            OnStateChanged?.Invoke();
        }
    }
}
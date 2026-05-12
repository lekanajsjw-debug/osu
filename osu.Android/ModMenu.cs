using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;

namespace osu.Android
{
    public class ModMenu
    {
        private bool isMenuVisible = false;
        private readonly Dictionary<string, bool> mods;
        private readonly Context context;

        public ModMenu(Context context)
        {
            this.context = context;
            mods = new Dictionary<string, bool>
            {
                { "Easy", false },
                { "NoFail", false },
                { "HalfTime", false },
                { "HardRock", false },
                { "DoubleTime", false },
                { "Hidden", false },
                { "Flashlight", false },
                { "Perfect", false }
            };
        }

        public void ToggleMenuVisibility()
        {
            isMenuVisible = !isMenuVisible;
            ShowToast(isMenuVisible ? "Mod Menu Opened" : "Mod Menu Closed");
        }

        public void SetModActive(string modName, bool isActive)
        {
            if (mods.ContainsKey(modName))
            {
                mods[modName] = isActive;
                ShowToast($"{modName} is now {(isActive ? "ON" : "OFF")}");
            }
            else
            {
                ShowToast("Invalid Mod Name");
            }
        }

        public bool IsModActive(string modName)
        {
            return mods.ContainsKey(modName) && mods[modName];
        }

        public List<string> GetActiveMods()
        {
            List<string> activeMods = new List<string>();
            foreach (var mod in mods)
            {
                if (mod.Value) activeMods.Add(mod.Key);
            }

            return activeMods;
        }

        private void ShowToast(string message)
        {
            Toast.MakeText(context, message, ToastLength.Short).Show();
        }
    }
}
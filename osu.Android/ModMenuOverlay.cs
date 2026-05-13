using System;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.OS;

namespace osu.Android
{
    public sealed class ModMenuOverlay : IDisposable
    {
        private readonly Context context;
        private readonly IWindowManager windowManager;

        private View trigger;
        private LinearLayout panel;

        private bool shown;

        public ModMenuOverlay(Context ctx)
        {
            context = ctx;
            windowManager = ctx.GetSystemService(Context.WindowService) as IWindowManager;

            CreateTrigger();
            CreatePanel();
        }

        // ===== INVISIBLE TRIGGER =====

        private void CreateTrigger()
        {
            trigger = new View(context);

            var p = new WindowManagerLayoutParams(
                1, 1,
                WindowManagerTypes.ApplicationOverlay,
                WindowManagerFlags.NotFocusable,
                PixelFormat.Translucent
            );

            p.Gravity = GravityFlags.Left | GravityFlags.Top;
            p.X = 0;
            p.Y = 0;

            trigger.Click += (s, e) =>
            {
                TogglePanel();
            };

            windowManager.AddView(trigger, p);
        }

        // ===== PANEL =====

        private void CreatePanel()
        {
            panel = new LinearLayout(context)
            {
                Orientation = Orientation.Vertical
            };

            panel.SetBackgroundColor(Color.Argb(180, 0, 0, 0));

            AddButton("AutoPlay", () => ModMenu.ToggleAutoPlay());
            AddButton("NoMiss", () => ModMenu.ToggleNoMiss());
            AddButton("Relax", () => ModMenu.ToggleRelax());
            AddButton("InstantSpin", () => ModMenu.ToggleInstantSpin());
            AddButton("Close", HidePanel);

            var p = new WindowManagerLayoutParams(
                WindowManagerLayoutParams.WrapContent,
                WindowManagerLayoutParams.WrapContent,
                WindowManagerTypes.ApplicationOverlay,
                WindowManagerFlags.NotFocusable,
                PixelFormat.Translucent
            );

            p.Gravity = GravityFlags.Center;

            panel.Visibility = ViewStates.Gone;

            windowManager.AddView(panel, p);
        }

        private void AddButton(string text, System.Action action)
        {
            var btn = new Button(context)
            {
                Text = text
            };

            btn.Click += (s, e) => action();
            panel.AddView(btn);
        }

        // ===== CONTROL =====

        private void TogglePanel()
        {
            if (shown) HidePanel();
            else ShowPanel();
        }

        private void ShowPanel()
        {
            panel.Visibility = ViewStates.Visible;
            shown = true;
        }

        private void HidePanel()
        {
            panel.Visibility = ViewStates.Gone;
            shown = false;
        }

        public void Dispose()
        {
            try
            {
                windowManager.RemoveView(trigger);
                windowManager.RemoveView(panel);
            }
            catch { }
        }
    }
}
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.OS;
using System;

namespace osu.Android
{
    public sealed class ModMenuOverlay : IDisposable
    {
        private readonly Context context;
        private readonly WindowManager windowManager;
        private Button button;
        private bool shown;

        public ModMenuOverlay(Context ctx)
        {
            context = ctx;
            windowManager = (WindowManager)context.GetSystemService(Context.WindowService);

            CreateButton();
        }

        private void CreateButton()
        {
            button = new Button(context)
            {
                Text = "MOD"
            };

            button.Click += (s, e) =>
            {
                ModMenu.SetAutoPlay(!ModMenu.AutoPlayEnabled);
            };

            var parameters = new WindowManagerLayoutParams(
                WindowManagerLayoutParams.WrapContent,
                WindowManagerLayoutParams.WrapContent,
                WindowManagerTypes.ApplicationOverlay,
                WindowManagerFlags.NotFocusable,
                Format.Translucent
            );

            parameters.Gravity = GravityFlags.Left | GravityFlags.Top;
            parameters.X = 20;
            parameters.Y = 200;

            windowManager.AddView(button, parameters);
            shown = true;
        }

        public void Dispose()
        {
            if (shown && button != null)
            {
                windowManager.RemoveView(button);
                button = null;
            }

            shown = false;
        }
    }
}
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;

namespace osu.Android
{
    public class ModMenuOverlay : Java.Lang.Object
    {
        private readonly Context context;

        private IWindowManager? windowManager;

        private LinearLayout? menuLayout;

        private ImageView? triggerButton;

        private bool menuVisible;

        private Button? autoPlayButton;
        private Button? noMissButton;
        private Button? relaxButton;
        private Button? instantSpinButton;

        public ModMenuOverlay(Context context)
        {
            this.context = context;
        }

        public void Show()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                if (!Settings.CanDrawOverlays(context))
                    return;
            }

            windowManager = context
                .GetSystemService(Context.WindowService)!
                .JavaCast<IWindowManager>();

            createTrigger();
            createMenu();

            ModMenu.OnStateChanged += updateButtons;
        }

        private void createTrigger()
        {
            triggerButton = new ImageView(context);

            triggerButton.SetBackgroundColor(Color.Argb(1, 255, 255, 255));

            var triggerParams = new WindowManagerLayoutParams(
                80,
                80,
                getOverlayType(),
                WindowManagerFlags.NotFocusable,
                Format.Translucent
            );

            triggerParams.Gravity = GravityFlags.Top | GravityFlags.Left;

            triggerParams.X = 0;
            triggerParams.Y = 300;

            triggerButton.Click += (_, _) =>
            {
                toggleMenu();
            };

            windowManager?.AddView(triggerButton, triggerParams);
        }

        private void createMenu()
        {
            menuLayout = new LinearLayout(context);

            menuLayout.Orientation = Orientation.Vertical;

            menuLayout.SetPadding(25, 25, 25, 25);

            menuLayout.SetBackgroundColor(Color.Argb(220, 20, 20, 20));

            autoPlayButton = createButton();
            noMissButton = createButton();
            relaxButton = createButton();
            instantSpinButton = createButton();

            autoPlayButton.Click += (_, _) =>
            {
                ModMenu.ToggleAutoPlay();
            };

            noMissButton.Click += (_, _) =>
            {
                ModMenu.ToggleNoMiss();
            };

            relaxButton.Click += (_, _) =>
            {
                ModMenu.ToggleRelax();
            };

            instantSpinButton.Click += (_, _) =>
            {
                ModMenu.ToggleInstantSpin();
            };

            menuLayout.AddView(autoPlayButton);
            menuLayout.AddView(noMissButton);
            menuLayout.AddView(relaxButton);
            menuLayout.AddView(instantSpinButton);

            var menuParams = new WindowManagerLayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent,
                getOverlayType(),
                WindowManagerFlags.NotFocusable,
                Format.Translucent
            );

            menuParams.Gravity = GravityFlags.Center;

            menuLayout.Visibility = ViewStates.Gone;

            windowManager?.AddView(menuLayout, menuParams);

            updateButtons();
        }

        private Button createButton()
        {
            var button = new Button(context);

            button.SetTextColor(Color.White);

            return button;
        }

        private void toggleMenu()
        {
            if (menuLayout == null)
                return;

            menuVisible = !menuVisible;

            menuLayout.Visibility = menuVisible
                ? ViewStates.Visible
                : ViewStates.Gone;
        }

        private void updateButtons()
        {
            if (autoPlayButton != null)
            {
                autoPlayButton.Text =
                    $"AutoPlay [{state(ModMenu.AutoPlayEnabled)}]";
            }

            if (noMissButton != null)
            {
                noMissButton.Text =
                    $"NoMiss [{state(ModMenu.NoMissEnabled)}]";
            }

            if (relaxButton != null)
            {
                relaxButton.Text =
                    $"Relax [{state(ModMenu.RelaxEnabled)}]";
            }

            if (instantSpinButton != null)
            {
                instantSpinButton.Text =
                    $"InstantSpin [{state(ModMenu.InstantSpinEnabled)}]";
            }
        }

        private string state(bool enabled)
        {
            return enabled ? "ON" : "OFF";
        }

        private WindowManagerTypes getOverlayType()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                return WindowManagerTypes.ApplicationOverlay;

            return WindowManagerTypes.Phone;
        }

        public void Dispose()
        {
            ModMenu.OnStateChanged -= updateButtons;

            if (triggerButton != null)
            {
                windowManager?.RemoveView(triggerButton);

                triggerButton.Dispose();

                triggerButton = null;
            }

            if (menuLayout != null)
            {
                windowManager?.RemoveView(menuLayout);

                menuLayout.Dispose();

                menuLayout = null;
            }

            autoPlayButton = null;
            noMissButton = null;
            relaxButton = null;
            instantSpinButton = null;
        }
    }
}
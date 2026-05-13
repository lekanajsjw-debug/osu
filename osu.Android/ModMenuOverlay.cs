// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace osu.Android
{
    /// <summary>
    /// Overlay для отображения меню модов поверх игры.
    /// Создаётся один раз, скрывается/показывается через Hide/Show.
    /// </summary>
    public class ModMenuOverlay : Java.Lang.Object, IDisposable
    {
        private readonly Context context;
        private IWindowManager? windowManager;
        private LinearLayout? menuLayout;
        private ImageView? triggerButton;
        private bool menuVisible;
        private bool isDisposed;

        // Кнопки модов
        private Button? autoPlayButton;
        private Button? noMissButton;
        private Button? relaxButton;
        private Button? instantSpinButton;
        private Button? forceRankedButton;
        private Button? resetAllButton;

        // Размер и позиция триггера
        private const int TRIGGER_SIZE = 80;
        private const int TRIGGER_X = 0;
        private const int TRIGGER_Y = 300;
        private const int MENU_PADDING = 25;
        private const int BUTTON_MARGIN = 10;

        public ModMenuOverlay(Context context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ── Публичные методы ───────────────────────────────────────────────

        /// <summary>
        /// Показать overlay меню. При первом вызове создаёт Views.
        /// При повторных — просто делает триггер видимым.
        /// </summary>
        public void Show()
        {
            if (isDisposed)
            {
                global::Android.Util.Log.Warn("ModMenuOverlay", "Attempted to show disposed overlay");
                return;
            }

            // Views уже созданы — просто показываем триггер
            if (menuLayout != null)
            {
                if (triggerButton != null)
                    triggerButton.Visibility = ViewStates.Visible;
                return;
            }

            // Проверка разрешения SYSTEM_ALERT_WINDOW (Android 6.0+)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && !Settings.CanDrawOverlays(context))
            {
                global::Android.Util.Log.Error("ModMenuOverlay", "Overlay permission denied. Grant SYSTEM_ALERT_WINDOW in settings.");
                return;
            }

            try
            {
                var windowService = context.GetSystemService(Context.WindowService);
                if (windowService == null)
                {
                    global::Android.Util.Log.Error("ModMenuOverlay", "WindowService is null");
                    return;
                }

                windowManager = windowService.JavaCast<IWindowManager>();
                if (windowManager == null)
                {
                    global::Android.Util.Log.Error("ModMenuOverlay", "Failed to cast to IWindowManager");
                    return;
                }

                createTrigger();
                createMenu();

                // OnStateChanged стреляет из ThreadPool → updateButtonsOnUiThread
                // маршалирует на UI поток
                ModMenu.OnStateChanged += updateButtonsOnUiThread;

                global::Android.Util.Log.Info("ModMenuOverlay", "Overlay shown successfully");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error showing overlay: {ex.Message}");
                cleanupViews();
            }
        }

        /// <summary>
        /// Скрыть overlay когда приложение уходит в фон.
        /// Views остаются в WindowManager — не пересоздаём при возврате.
        /// </summary>
        public void Hide()
        {
            if (isDisposed) return;

            try
            {
                if (triggerButton != null)
                    triggerButton.Visibility = ViewStates.Gone;

                if (menuLayout != null)
                {
                    menuLayout.Visibility = ViewStates.Gone;
                    menuVisible = false;
                }
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error hiding overlay: {ex.Message}");
            }
        }

        // ── Создание Views ─────────────────────────────────────────────────

        private void createTrigger()
        {
            if (windowManager == null) return;

            triggerButton = new ImageView(context);
            triggerButton.SetBackgroundColor(Color.Argb(40, 255, 255, 255));

            var p = new WindowManagerLayoutParams(
                TRIGGER_SIZE,
                TRIGGER_SIZE,
                getOverlayType(),
                WindowManagerFlags.NotFocusable,
                Format.Translucent)
            {
                Gravity = GravityFlags.Top | GravityFlags.Start,
                X = TRIGGER_X,
                Y = TRIGGER_Y
            };

            triggerButton.Click += onTriggerClick;
            windowManager.AddView(triggerButton, p);
        }

        private void createMenu()
        {
            if (windowManager == null) return;

            menuLayout = new LinearLayout(context) { Orientation = Orientation.Vertical };
            menuLayout.SetPadding(MENU_PADDING, MENU_PADDING, MENU_PADDING, MENU_PADDING);
            menuLayout.SetBackgroundColor(Color.Argb(220, 20, 20, 20));

            autoPlayButton    = createButton("AutoPlay");
            noMissButton      = createButton("NoMiss");
            relaxButton       = createButton("Relax");
            instantSpinButton = createButton("InstantSpin");
            forceRankedButton = createButton("ForceRanked");
            resetAllButton    = createButton("Reset All", Color.Argb(255, 180, 50, 50));

            autoPlayButton.Click    += (_, _) => ModMenu.ToggleAutoPlay();
            noMissButton.Click      += (_, _) => ModMenu.ToggleNoMiss();
            relaxButton.Click       += (_, _) => ModMenu.ToggleRelax();
            instantSpinButton.Click += (_, _) => ModMenu.ToggleInstantSpin();
            forceRankedButton.Click += (_, _) => ModMenu.ToggleForceRanked();
            resetAllButton.Click    += onResetAllClick;

            addButtonToLayout(autoPlayButton);
            addButtonToLayout(noMissButton);
            addButtonToLayout(relaxButton);
            addButtonToLayout(instantSpinButton);
            addButtonToLayout(forceRankedButton);
            addButtonToLayout(resetAllButton);

            var menuParams = new WindowManagerLayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent,
                getOverlayType(),
                WindowManagerFlags.NotFocusable,
                Format.Translucent)
            {
                Gravity = GravityFlags.Center
            };

            menuLayout.Visibility = ViewStates.Gone;
            windowManager.AddView(menuLayout, menuParams);

            // Первое обновление — мы на UI потоке, вызываем напрямую
            updateButtons();
        }

        private Button createButton(string text, Color? backgroundColor = null)
        {
            var btn = new Button(context);
            btn.SetTextColor(Color.White);
            btn.Text = text;

            if (backgroundColor.HasValue)
                btn.SetBackgroundColor(backgroundColor.Value);

            return btn;
        }

        private void addButtonToLayout(Button button)
        {
            var lp = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent);
            lp.SetMargins(0, BUTTON_MARGIN, 0, BUTTON_MARGIN);
            menuLayout?.AddView(button, lp);
        }

        // ── Обработчики событий ────────────────────────────────────────────

        private void onTriggerClick(object? sender, EventArgs e)
        {
            try { toggleMenu(); }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error toggling menu: {ex.Message}");
            }
        }

        private void onResetAllClick(object? sender, EventArgs e)
        {
            try { ModMenu.ResetAll(); }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error resetting mods: {ex.Message}");
            }
        }

        private void toggleMenu()
        {
            if (menuLayout == null) return;

            menuVisible = !menuVisible;
            menuLayout.Visibility = menuVisible ? ViewStates.Visible : ViewStates.Gone;
        }

        // ── Обновление кнопок ──────────────────────────────────────────────

        /// <summary>
        /// Маршалирует updateButtons на UI поток.
        /// OnStateChanged стреляет из ThreadPool — трогать View оттуда нельзя.
        /// </summary>
        private void updateButtonsOnUiThread()
        {
            if (context is Activity activity)
                activity.RunOnUiThread(updateButtons);
            else
            {
                global::Android.Util.Log.Warn("ModMenuOverlay", "Context is not an Activity, updating directly");
                updateButtons();
            }
        }

        /// <summary>
        /// Обновить текст и состояние кнопок.
        /// Вызывать ТОЛЬКО из UI потока.
        /// </summary>
        private void updateButtons()
        {
            try
            {
                if (autoPlayButton != null)
                    autoPlayButton.Text = $"AutoPlay [{stateText(ModMenu.AutoPlayEnabled)}]";

                if (noMissButton != null)
                {
                    noMissButton.Text    = $"NoMiss [{stateText(ModMenu.NoMissEnabled)}]";
                    noMissButton.Enabled = !ModMenu.AutoPlayEnabled;
                }

                if (relaxButton != null)
                    relaxButton.Text = $"Relax [{stateText(ModMenu.RelaxEnabled)}]";

                if (instantSpinButton != null)
                    instantSpinButton.Text = $"InstantSpin [{stateText(ModMenu.InstantSpinEnabled)}]";

                if (forceRankedButton != null)
                    forceRankedButton.Text = $"ForceRanked [{stateText(ModMenu.ForceRankedEnabled)}]";
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error updating buttons: {ex.Message}");
            }
        }

        private static string stateText(bool enabled) => enabled ? "ON" : "OFF";

        // ── Хелперы ────────────────────────────────────────────────────────

        /// <summary>
        /// Правильный тип overlay в зависимости от версии Android.
        /// </summary>
        private WindowManagerTypes getOverlayType()
        {
            // Android 6.0+ (API 23+) — обязательно ApplicationOverlay
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                return WindowManagerTypes.ApplicationOverlay;

            // Android < 6.0 — устаревший тип только для совместимости
#pragma warning disable CS0618
            return WindowManagerTypes.Phone;
#pragma warning restore CS0618
        }

        // ── Cleanup / Dispose ──────────────────────────────────────────────

        private void cleanupViews()
        {
            try
            {
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

                autoPlayButton    = null;
                noMissButton      = null;
                relaxButton       = null;
                instantSpinButton = null;
                forceRankedButton = null;
                resetAllButton    = null;

                // windowManager получен через JavaCast — явно освобождаем
                windowManager?.Dispose();
                windowManager = null;
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error cleaning up views: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                try
                {
                    ModMenu.OnStateChanged -= updateButtonsOnUiThread;
                    cleanupViews();
                    global::Android.Util.Log.Info("ModMenuOverlay", "Disposed successfully");
                }
                catch (Exception ex)
                {
                    global::Android.Util.Log.Error("ModMenuOverlay", $"Error during dispose: {ex.Message}");
                }
            }

            isDisposed = true;
            base.Dispose(disposing);
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

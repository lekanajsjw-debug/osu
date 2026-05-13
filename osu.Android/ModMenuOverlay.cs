// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
    /// Overlay для отображения меню модов поверх игры
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

        // Параметры overlay
        private const int TRIGGER_SIZE = 80;
        private const int TRIGGER_X = 0;
        private const int TRIGGER_Y = 300;
        private const int MENU_PADDING = 25;
        private const int BUTTON_MARGIN = 10;

        public ModMenuOverlay(Context context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Показать overlay меню
        /// </summary>
        public void Show()
        {
            if (isDisposed)
            {
                global::Android.Util.Log.Warn("ModMenuOverlay", "Attempted to show disposed overlay");
                return;
            }

            if (menuLayout != null)
            {
                global::Android.Util.Log.Debug("ModMenuOverlay", "Overlay already shown");
                return;
            }

            // Проверка разрешений для Android 6.0+
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                if (!Settings.CanDrawOverlays(context))
                {
                    global::Android.Util.Log.Error(
                        "ModMenuOverlay",
                        "Overlay permission denied. Please grant permission in settings."
                    );
                    return;
                }
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

                // Подписка на события изменения состояния модов
                ModMenu.OnStateChanged += updateButtons;

                global::Android.Util.Log.Info("ModMenuOverlay", "Overlay shown successfully");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error showing overlay: {ex.Message}");
                cleanupViews();
            }
        }

        /// <summary>
        /// Создать кнопку-триггер для открытия меню
        /// </summary>
        private void createTrigger()
        {
            if (windowManager == null) return;

            try
            {
                triggerButton = new ImageView(context);
                triggerButton.SetBackgroundColor(Color.Argb(40, 255, 255, 255));

                var triggerParams = new WindowManagerLayoutParams(
                    TRIGGER_SIZE,
                    TRIGGER_SIZE,
                    getOverlayType(),
                    WindowManagerFlags.NotFocusable,
                    Format.Translucent
                )
                {
                    Gravity = GravityFlags.Top | GravityFlags.Start,
                    X = TRIGGER_X,
                    Y = TRIGGER_Y
                };

                triggerButton.Click += onTriggerClick;
                windowManager.AddView(triggerButton, triggerParams);
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error creating trigger: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Создать основное меню с кнопками модов
        /// </summary>
        private void createMenu()
        {
            if (windowManager == null) return;

            try
            {
                menuLayout = new LinearLayout(context)
                {
                    Orientation = Orientation.Vertical
                };

                menuLayout.SetPadding(MENU_PADDING, MENU_PADDING, MENU_PADDING, MENU_PADDING);
                menuLayout.SetBackgroundColor(Color.Argb(220, 20, 20, 20));

                // Создание кнопок
                autoPlayButton = createButton("AutoPlay");
                noMissButton = createButton("NoMiss");
                relaxButton = createButton("Relax");
                instantSpinButton = createButton("InstantSpin");
                forceRankedButton = createButton("ForceRanked");
                resetAllButton = createButton("Reset All", Color.Argb(255, 180, 50, 50));

                // Назначение обработчиков
                autoPlayButton.Click += (_, _) => ModMenu.ToggleAutoPlay();
                noMissButton.Click += (_, _) => ModMenu.ToggleNoMiss();
                relaxButton.Click += (_, _) => ModMenu.ToggleRelax();
                instantSpinButton.Click += (_, _) => ModMenu.ToggleInstantSpin();
                forceRankedButton.Click += (_, _) => ModMenu.ToggleForceRanked();
                resetAllButton.Click += onResetAllClick;

                // Добавление кнопок в layout
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
                    Format.Translucent
                )
                {
                    Gravity = GravityFlags.Center
                };

                menuLayout.Visibility = ViewStates.Gone;
                windowManager.AddView(menuLayout, menuParams);

                updateButtons();
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error creating menu: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Создать кнопку с заданным текстом и цветом
        /// </summary>
        private Button createButton(string text = "", Color? backgroundColor = null)
        {
            var button = new Button(context);
            button.SetTextColor(Color.White);
            button.Text = text;

            if (backgroundColor.HasValue)
            {
                button.SetBackgroundColor(backgroundColor.Value);
            }

            return button;
        }

        /// <summary>
        /// Добавить кнопку в layout с отступами
        /// </summary>
        private void addButtonToLayout(Button button)
        {
            var layoutParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            layoutParams.SetMargins(0, BUTTON_MARGIN, 0, BUTTON_MARGIN);
            menuLayout?.AddView(button, layoutParams);
        }

        /// <summary>
        /// Обработчик клика по триггер-кнопке
        /// </summary>
        private void onTriggerClick(object? sender, EventArgs e)
        {
            try
            {
                toggleMenu();
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error toggling menu: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик кнопки "Reset All"
        /// </summary>
        private void onResetAllClick(object? sender, EventArgs e)
        {
            try
            {
                ModMenu.ResetAll();
                global::Android.Util.Log.Info("ModMenuOverlay", "All mods reset");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error resetting mods: {ex.Message}");
            }
        }

        /// <summary>
        /// Переключить видимость меню
        /// </summary>
        private void toggleMenu()
        {
            if (menuLayout == null) return;

            menuVisible = !menuVisible;
            menuLayout.Visibility = menuVisible ? ViewStates.Visible : ViewStates.Gone;

            global::Android.Util.Log.Debug("ModMenuOverlay", $"Menu visibility: {menuVisible}");
        }

        /// <summary>
        /// Обновить текст на всех кнопках согласно текущему состоянию модов
        /// </summary>
        private void updateButtons()
        {
            try
            {
                if (autoPlayButton != null)
                    autoPlayButton.Text = $"AutoPlay [{getStateText(ModMenu.AutoPlayEnabled)}]";

                if (noMissButton != null)
                {
                    noMissButton.Text = $"NoMiss [{getStateText(ModMenu.NoMissEnabled)}]";
                    // Блокируем кнопку если AutoPlay активен
                    noMissButton.Enabled = !ModMenu.AutoPlayEnabled || !ModMenu.NoMissEnabled;
                }

                if (relaxButton != null)
                    relaxButton.Text = $"Relax [{getStateText(ModMenu.RelaxEnabled)}]";

                if (instantSpinButton != null)
                    instantSpinButton.Text = $"InstantSpin [{getStateText(ModMenu.InstantSpinEnabled)}]";

                if (forceRankedButton != null)
                    forceRankedButton.Text = $"ForceRanked [{getStateText(ModMenu.ForceRankedEnabled)}]";
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error updating buttons: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить текстовое представление состояния мода
        /// </summary>
        private string getStateText(bool enabled) => enabled ? "ON" : "OFF";

        /// <summary>
        /// Получить правильный тип overlay в зависимости от версии Android
        /// </summary>
        private WindowManagerTypes getOverlayType()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                return WindowManagerTypes.ApplicationOverlay;

            // Для Android 6-7 используем ApplicationOverlay если доступен
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                return WindowManagerTypes.ApplicationOverlay;

            // Для старых версий (не рекомендуется, но для совместимости)
#pragma warning disable CS0618 // Type or member is obsolete
            return WindowManagerTypes.Phone;
#pragma warning restore CS0618
        }

        /// <summary>
        /// Очистка всех view элементов
        /// </summary>
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

                // Обнуляем ссылки на кнопки
                autoPlayButton = null;
                noMissButton = null;
                relaxButton = null;
                instantSpinButton = null;
                forceRankedButton = null;
                resetAllButton = null;
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenuOverlay", $"Error cleaning up views: {ex.Message}");
            }
        }

        /// <summary>
        /// Dispose pattern implementation
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                try
                {
                    // Отписываемся от событий
                    ModMenu.OnStateChanged -= updateButtons;

                    // Очищаем views
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

        /// <summary>
        /// Public Dispose method
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

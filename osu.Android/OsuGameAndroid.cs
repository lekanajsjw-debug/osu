// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Microsoft.Maui.Devices;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Platform;
using osu.Game;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens;
using osu.Game.Updater;
using osu.Game.Utils;
using osuTK;

namespace osu.Android
{
    public partial class OsuGameAndroid : OsuGame
    {
        [Cached]
        private readonly OsuGameActivity gameActivity;

        private readonly PackageInfo packageInfo;

        // Флаг для отслеживания подписки на изменения модов
        private bool isModIntegrationSetup;

        public override Vector2 ScalingContainerTargetDrawSize => new Vector2(1024, 1024 * DrawHeight / DrawWidth);

        public OsuGameAndroid(OsuGameActivity activity)
            : base(null)
        {
            gameActivity = activity;
            packageInfo = Application.Context.ApplicationContext!.PackageManager!.GetPackageInfo(Application.Context.ApplicationContext.PackageName!, 0).AsNonNull();
        }

        public override string Version
        {
            get
            {
                if (!IsDeployedBuild)
                    return @"local " + (DebugUtils.IsDebugBuild ? @"debug" : @"release");

                return packageInfo.VersionName.AsNonNull();
            }
        }

        public override Version AssemblyVersion => new Version(packageInfo.VersionName.AsNonNull().Split('-').First());

        protected override void LoadComplete()
        {
            base.LoadComplete();
            
            UserPlayingState.BindValueChanged(_ => updateOrientation());

            // Инициализация интеграции с ModMenu
            setupModIntegration();
        }

        /// <summary>
        /// Настройка интеграции ModMenu с системой модов игры
        /// </summary>
        private void setupModIntegration()
        {
            if (isModIntegrationSetup)
                return;

            try
            {
                // Подписываемся на изменения состояния ModMenu
                ModMenu.OnStateChanged += onModMenuStateChanged;
                
                isModIntegrationSetup = true;

                global::Android.Util.Log.Info("OsuGameAndroid", "ModMenu integration setup completed");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("OsuGameAndroid", $"Failed to setup ModMenu integration: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик изменения состояния ModMenu
        /// </summary>
        private void onModMenuStateChanged()
        {
            try
            {
                // Применяем моды при изменении состояния
                Schedule(() => applyModMenuMods());
                
                global::Android.Util.Log.Debug("OsuGameAndroid", $"ModMenu state changed: {ModMenu.GetDebugInfo()}");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("OsuGameAndroid", $"Error handling ModMenu state change: {ex.Message}");
            }
        }

        /// <summary>
        /// Применить моды из ModMenu к текущему рулсету
        /// </summary>
        private void applyModMenuMods()
        {
            try
            {
                // Получаем текущий рулсет
                var ruleset = Ruleset.Value.CreateInstance();
                if (ruleset == null)
                {
                    global::Android.Util.Log.Warn("OsuGameAndroid", "Cannot apply mods: ruleset is null");
                    return;
                }

                // Получаем доступные моды для текущего рулсета
                var availableMods = ruleset.CreateAllMods().ToArray();
                
                // Создаем список модов для применения
                var modsToApply = new List<Mod>();

                // Добавляем текущие активные моды (не из ModMenu)
                var currentMods = SelectedMods.Value.ToList();
                
                // Удаляем моды, которые управляются через ModMenu, чтобы избежать дублирования
                currentMods.RemoveAll(m => 
                    m is ModAutoplay || 
                    m is ModNoFail || 
                    m is ModRelax ||
                    m.GetType().Name.Contains("InstantSpin") ||
                    m.GetType().Name.Contains("ForceRanked"));

                // Добавляем моды из ModMenu
                if (ModMenu.AutoPlayEnabled)
                {
                    var autoplayMod = availableMods.OfType<ModAutoplay>().FirstOrDefault();
                    if (autoplayMod != null)
                    {
                        modsToApply.Add(autoplayMod);
                        global::Android.Util.Log.Debug("OsuGameAndroid", "Applied AutoPlay mod");
                    }
                }

                if (ModMenu.NoMissEnabled)
                {
                    var noFailMod = availableMods.OfType<ModNoFail>().FirstOrDefault();
                    if (noFailMod != null)
                    {
                        modsToApply.Add(noFailMod);
                        global::Android.Util.Log.Debug("OsuGameAndroid", "Applied NoFail mod");
                    }
                }

                if (ModMenu.RelaxEnabled)
                {
                    var relaxMod = availableMods.OfType<ModRelax>().FirstOrDefault();
                    if (relaxMod != null)
                    {
                        modsToApply.Add(relaxMod);
                        global::Android.Util.Log.Debug("OsuGameAndroid", "Applied Relax mod");
                    }
                }

                // InstantSpin и ForceRanked могут быть кастомными модами
                // Их реализация зависит от вашей конкретной логики
                if (ModMenu.InstantSpinEnabled)
                {
                    // TODO: Добавить реализацию InstantSpin мода
                    global::Android.Util.Log.Debug("OsuGameAndroid", "InstantSpin enabled (implementation pending)");
                }

                if (ModMenu.ForceRankedEnabled)
                {
                    // TODO: Добавить реализацию ForceRanked мода
                    global::Android.Util.Log.Debug("OsuGameAndroid", "ForceRanked enabled (implementation pending)");
                }

                // Объединяем существующие моды с модами из ModMenu
                currentMods.AddRange(modsToApply);

                // Применяем обновленный список модов
                if (!SelectedMods.Disabled)
                {
                    SelectedMods.Value = currentMods.ToArray();
                    global::Android.Util.Log.Info("OsuGameAndroid", $"Applied {modsToApply.Count} mods from ModMenu");
                }
                else
                {
                    global::Android.Util.Log.Warn("OsuGameAndroid", "Cannot apply mods: SelectedMods is disabled");
                }
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("OsuGameAndroid", $"Error applying ModMenu mods: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Переопределяем смену экрана для применения модов при входе в игру
        /// </summary>
        protected override void ScreenChanged(IOsuScreen? current, IOsuScreen? newScreen)
        {
            base.ScreenChanged(current, newScreen);

            if (newScreen != null)
            {
                updateOrientation();

                // Если переходим на экран игры, применяем моды из ModMenu
                if (newScreen.GetType().Name.Contains("Player"))
                {
                    try
                    {
                        applyModMenuMods();
                        global::Android.Util.Log.Info("OsuGameAndroid", "Applied ModMenu mods on Player screen enter");
                    }
                    catch (Exception ex)
                    {
                        global::Android.Util.Log.Error("OsuGameAndroid", $"Error applying mods on screen change: {ex.Message}");
                    }
                }
            }
        }

        private void updateOrientation()
        {
            var orientation = MobileUtils.GetOrientation(this, (IOsuScreen)ScreenStack.CurrentScreen, gameActivity.IsTablet);

            switch (orientation)
            {
                case MobileUtils.Orientation.Locked:
                    gameActivity.RequestedOrientation = ScreenOrientation.Locked;
                    break;

                case MobileUtils.Orientation.Portrait:
                    gameActivity.RequestedOrientation = ScreenOrientation.Portrait;
                    break;

                case MobileUtils.Orientation.Default:
                    gameActivity.RequestedOrientation = gameActivity.DefaultOrientation;
                    break;
            }
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);
            host.Window.CursorState |= CursorState.Hidden;
        }

        protected override UpdateManager CreateUpdateManager() => new MobileUpdateNotifier();

        protected override BatteryInfo CreateBatteryInfo() => new AndroidBatteryInfo();

        /// <summary>
        /// Очистка ресурсов при закрытии игры
        /// </summary>
        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing && isModIntegrationSetup)
            {
                try
                {
                    ModMenu.OnStateChanged -= onModMenuStateChanged;
                    global::Android.Util.Log.Info("OsuGameAndroid", "ModMenu integration disposed");
                }
                catch (Exception ex)
                {
                    global::Android.Util.Log.Error("OsuGameAndroid", $"Error disposing ModMenu integration: {ex.Message}");
                }
            }

            base.Dispose(isDisposing);
        }

        private class AndroidBatteryInfo : BatteryInfo
        {
            public override double? ChargeLevel => Battery.ChargeLevel;

            public override bool OnBattery => Battery.PowerSource == BatteryPowerSource.Battery;
        }
    }
}

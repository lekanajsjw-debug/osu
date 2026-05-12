using Android.App;
using Android.Content;
using Android.Widget;
using HarmonyLib;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Judgements;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.UI;

namespace osu.Android
{
    /// <summary>
    /// ModMenu - система модификаций для osu! Android
    /// Использует Harmony для динамического патчинга игровой логики
    /// </summary>
    public class ModMenu : IDisposable
    {
        private readonly Context context;
        private static ModMenu instance;
        private Harmony harmony;
        private bool isDisposed;
        
        // Статические настройки модов (доступны из Harmony патчей)
        public static bool AutoPlayEnabled { get; private set; }
        public static bool NoMissEnabled { get; private set; }
        public static bool InstantSpinEnabled { get; private set; }
        public static bool RelaxEnabled { get; private set; }
        public static float AccuracyMultiplier { get; set; } = 1.0f;
        public static float ApproachRate { get; set; } = -1f;
        public static float HitBoxMultiplier { get; set; } = 1.5f;
        
        // Режимы сабмита скора
        public enum ScoreSubmissionMode
        {
            Normal,      // Обычный сабмит с видимыми модами
            Clean,       // Скрытые моды в сабмите
            Offline      // Локальное сохранение (без сабмита)
        }
        
        public static ScoreSubmissionMode SubmissionMode { get; set; } = ScoreSubmissionMode.Normal;

        public ModMenu(Context context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            
            this.context = context;
            instance = this;
            isDisposed = false;
            
            try
            {
                InitializeHarmonyPatches();
            }
            catch (Exception ex)
            {
                ShowToast($"ModMenu: Ошибка инициализации - {ex.Message}");
            }
        }

        private void InitializeHarmonyPatches()
        {
            try
            {
                harmony = new Harmony("com.osu.modmenu.patches");
                
                // Патч ScoreProcessor для обработки результатов попаданий
                PatchMethod(
                    typeof(ScoreProcessor),
                    "ApplyResult",
                    BindingFlags.Public | BindingFlags.Instance,
                    nameof(OnApplyResultPrefix),
                    null
                );
                
                // Патч HealthProcessor для управления здоровьем
                PatchMethod(
                    typeof(HealthProcessor),
                    "ApplyResult",
                    BindingFlags.Public | BindingFlags.Instance,
                    nameof(OnHealthApplyResultPrefix),
                    null
                );
                
                // Патч для InstantSpin - мгновенное завершение спиннеров
                var osuScoreProcessor = Type.GetType("osu.Game.Rulesets.Osu.Scoring.OsuScoreProcessor, osu.Game.Rulesets.Osu");
                if (osuScoreProcessor != null)
                {
                    PatchMethod(
                        osuScoreProcessor,
                        "SimulateAutoplay",
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        nameof(OnSimulateAutoplayPrefix),
                        null
                    );
                }
                
                // Патч для перехвата сабмита скора
                var scoreManager = Type.GetType("osu.Game.Scoring.ScoreManager, osu.Game");
                if (scoreManager != null)
                {
                    PatchMethod(
                        scoreManager,
                        "Submit",
                        BindingFlags.Public | BindingFlags.Instance,
                        nameof(OnScoreSubmitPrefix),
                        null
                    );
                }
                
                ShowToast("ModMenu: Патчи успешно применены");
            }
            catch (Exception ex)
            {
                ShowToast($"ModMenu: Ошибка при патчинге - {ex.Message}");
                harmony?.UnpatchAll("com.osu.modmenu.patches");
                throw;
            }
        }
        
        /// <summary>
        /// Вспомогательный метод для безопасного патчинга
        /// </summary>
        private void PatchMethod(Type targetType, string methodName, BindingFlags flags, 
            string prefixMethodName, string postfixMethodName)
        {
            try
            {
                var method = targetType?.GetMethod(methodName, flags);
                if (method == null)
                {
                    ShowToast($"Метод не найден: {targetType?.Name}.{methodName}");
                    return;
                }

                var prefixMethod = prefixMethodName != null 
                    ? new HarmonyMethod(typeof(ModMenu), prefixMethodName) 
                    : null;
                    
                var postfixMethod = postfixMethodName != null 
                    ? new HarmonyMethod(typeof(ModMenu), postfixMethodName) 
                    : null;

                harmony.Patch(method, prefix: prefixMethod, postfix: postfixMethod);
            }
            catch (Exception ex)
            {
                ShowToast($"Ошибка патчинга {methodName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Хук ScoreProcessor - управление точностью и миссами
        /// </summary>
        public static bool OnApplyResultPrefix(ScoreProcessor __instance, JudgementResult result)
        {
            try
            {
                if (result == null) return true;
                
                // NoMiss - превращаем миссы в 50
                if (NoMissEnabled && result.Type == HitResult.Miss)
                {
                    ModifyJudgementResult(result, HitResult.Meh, 0.0);
                    return true;
                }
                
                // Relax - автоматическая идеальная точность
                if (RelaxEnabled && result.Type != HitResult.Miss)
                {
                    ModifyJudgementResult(result, HitResult.Great, 0.0);
                    return true;
                }
                
                // AccuracyMultiplier - модификатор точности
                if (Math.Abs(AccuracyMultiplier - 1.0f) > 0.01f && AccuracyMultiplier > 0)
                {
                    // Масштабируем результат в зависимости от множителя
                    if (AccuracyMultiplier < 1.0f && result.Type == HitResult.Great)
                    {
                        ModifyJudgementResult(result, HitResult.Ok, result.TimeOffset);
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnApplyResultPrefix error: {ex.Message}");
                return true;
            }
        }
        
        /// <summary>
        /// Хук HealthProcessor - управление здоровьем игрока
        /// </summary>
        public static bool OnHealthApplyResultPrefix(HealthProcessor __instance, JudgementResult result)
        {
            try
            {
                if (__instance == null || result == null) return true;
                
                if (NoMissEnabled || AutoPlayEnabled)
                {
                    // Восстанавливаем здоровье при неправильном результате
                    ModifyHealth(__instance, 0.2f);
                    return false; // Прерываем обычную обработку урона
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnHealthApplyResultPrefix error: {ex.Message}");
                return true;
            }
        }
        
        /// <summary>
        /// Хук для InstantSpin - мгновенное завершение спиннеров
        /// </summary>
        public static bool OnSimulateAutoplayPrefix(object __instance, HitObject hitObject)
        {
            try
            {
                if (!InstantSpinEnabled || hitObject == null) return true;
                
                // Проверяем тип объекта
                var spinnerType = Type.GetType("osu.Game.Rulesets.Osu.Objects.Spinner, osu.Game.Rulesets.Osu");
                if (spinnerType != null && spinnerType.IsAssignableFrom(hitObject.GetType()))
                {
                    // Принудительно завершаем спиннер
                    var completeMethod = hitObject.GetType().GetMethod("Complete",
                        BindingFlags.Public | BindingFlags.Instance);
                    completeMethod?.Invoke(hitObject, null);
                    
                    return false; // Прерываем обычную симуляцию
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnSimulateAutoplayPrefix error: {ex.Message}");
                return true;
            }
        }
        
        /// <summary>
        /// Перехват сабмита скора - управление видимостью модов
        /// </summary>
        public static bool OnScoreSubmitPrefix(object __instance, object score)
        {
            try
            {
                if (score == null) return true;
                
                switch (SubmissionMode)
                {
                    case ScoreSubmissionMode.Offline:
                        // Блокируем онлайн сабмит
                        return false;
                        
                    case ScoreSubmissionMode.Clean:
                        // Скрываем информацию о модах перед сабмитом
                        ClearScoreMods(score);
                        if (NoMissEnabled || AutoPlayEnabled)
                            ClearScoreStatistics(score);
                        return true;
                        
                    default:
                        return true; // Нормальный сабмит
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnScoreSubmitPrefix error: {ex.Message}");
                return true;
            }
        }
        
        // ============ Вспомогательные методы для рефлексии ============
        
        /// <summary>
        /// Модифицирует результат попадания через рефлексию
        /// </summary>
        private static void ModifyJudgementResult(JudgementResult result, HitResult newType, double newTimeOffset)
        {
            try
            {
                var typeField = typeof(JudgementResult).GetField("type", 
                    BindingFlags.Instance | BindingFlags.NonPublic);
                typeField?.SetValue(result, newType);
                
                var timeField = typeof(JudgementResult).GetField("timeOffset",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                timeField?.SetValue(result, newTimeOffset);
            }
            catch { /* Игнорируем ошибки рефлексии */ }
        }
        
        /// <summary>
        /// Модифицирует здоровье игрока
        /// </summary>
        private static void ModifyHealth(HealthProcessor processor, float addHealth)
        {
            try
            {
                var healthField = typeof(HealthProcessor).GetField("health",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                
                if (healthField != null)
                {
                    float currentHealth = (float)healthField.GetValue(processor);
                    float newHealth = Math.Min(currentHealth + addHealth, 1.0f);
                    healthField.SetValue(processor, newHealth);
                }
            }
            catch { /* Игнорируем ошибки */ }
        }
        
        /// <summary>
        /// Очищает моды из информации о скоре
        /// </summary>
        private static void ClearScoreMods(object score)
        {
            try
            {
                var modsProperty = score.GetType().GetProperty("Mods",
                    BindingFlags.Public | BindingFlags.Instance);
                if (modsProperty != null && modsProperty.CanWrite)
                {
                    modsProperty.SetValue(score, new List<object>());
                }
            }
            catch { /* Игнорируем ошибки */ }
        }
        
        /// <summary>
        /// Очищает статистику скора
        /// </summary>
        private static void ClearScoreStatistics(object score)
        {
            try
            {
                var statsProperty = score.GetType().GetProperty("Statistics",
                    BindingFlags.Public | BindingFlags.Instance);
                if (statsProperty != null)
                {
                    var stats = statsProperty.GetValue(score) as Dictionary<HitResult, int>;
                    if (stats != null)
                    {
                        stats[HitResult.Miss] = 0;
                    }
                }
            }
            catch { /* Игнорируем ошибки */ }
        }
        
        // ============ Публичные методы управления модами ============
        
        public void SetAutoPlay(bool enabled)
        {
            AutoPlayEnabled = enabled;
            if (enabled)
            {
                NoMissEnabled = true;
                RelaxEnabled = false;
                SubmissionMode = ScoreSubmissionMode.Offline;
            }
            ShowToast($"AutoPlay: {(enabled ? "✓ ON" : "✗ OFF")}");
        }
        
        public void SetNoMiss(bool enabled)
        {
            NoMissEnabled = enabled;
            if (!enabled && AutoPlayEnabled) AutoPlayEnabled = false;
            ShowToast($"NoMiss: {(enabled ? "✓ ON" : "✗ OFF")}");
        }
        
        public void SetRelax(bool enabled)
        {
            RelaxEnabled = enabled;
            if (enabled)
            {
                SubmissionMode = ScoreSubmissionMode.Clean;
                AutoPlayEnabled = false;
            }
            ShowToast($"Relax: {(enabled ? "✓ ON" : "✗ OFF")}");
        }
        
        public void SetInstantSpin(bool enabled)
        {
            InstantSpinEnabled = enabled;
            ShowToast($"InstantSpin: {(enabled ? "✓ ON" : "✗ OFF")}");
        }
        
        public void SetSubmissionMode(ScoreSubmissionMode mode)
        {
            SubmissionMode = mode;
            string modeName = mode switch
            {
                ScoreSubmissionMode.Clean => "Clean",
                ScoreSubmissionMode.Offline => "Offline",
                _ => "Normal"
            };
            ShowToast($"Submission: {modeName}");
        }
        
        public void SetAccuracy(float percent)
        {
            if (percent <= 0 || percent > 100) return;
            AccuracyMultiplier = percent / 100f;
            ShowToast($"Accuracy: {percent}%");
        }
        
        public void SetApproachRate(float ar)
        {
            if (ar < -1 || ar > 11) return;
            ApproachRate = ar;
            ShowToast($"AR: {ar}");
        }
        
        /// <summary>
        /// Получить текущее состояние всех модов
        /// </summary>
        public Dictionary<string, bool> GetModsState()
        {
            return new Dictionary<string, bool>
            {
                { "AutoPlay", AutoPlayEnabled },
                { "NoMiss", NoMissEnabled },
                { "Relax", RelaxEnabled },
                { "InstantSpin", InstantSpinEnabled }
            };
        }
        
        private void ShowToast(string message)
        {
            try
            {
                Toast.MakeText(context, message, ToastLength.Short)?.Show();
            }
            catch { /* Игнорируем ошибки Toast */ }
        }
        
        // ============ IDisposable реализация ============
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;
            
            if (disposing)
            {
                try
                {
                    harmony?.UnpatchAll("com.osu.modmenu.patches");
                    harmony = null;
                }
                catch { /* Игнорируем ошибки при очистке */ }
            }
            
            isDisposed = true;
        }
        
        ~ModMenu()
        {
            Dispose(false);
        }
    }
}

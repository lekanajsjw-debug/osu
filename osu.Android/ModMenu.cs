using Android.App;
using Android.Content;
using Android.Widget;
using HarmonyLib;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace osu.Android
{
    public sealed class ModMenu : IDisposable
    {
        private readonly Context context;
        private readonly Harmony harmony;

        private bool disposed;

        public static bool AutoPlayEnabled { get; private set; }
        public static bool NoMissEnabled { get; private set; }
        public static bool RelaxEnabled { get; private set; }
        public static bool InstantSpinEnabled { get; private set; }

        public enum ScoreSubmissionMode
        {
            Normal,
            Clean,
            Offline
        }

        public static ScoreSubmissionMode SubmissionMode { get; set; }
            = ScoreSubmissionMode.Normal;

        public ModMenu(Context ctx)
        {
            context = ctx ?? throw new ArgumentNullException(nameof(ctx));

            harmony = new Harmony("com.osu.modmenu");

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                PatchMethod(
                    typeof(ScoreProcessor),
                    "ApplyResult",
                    new[] { typeof(JudgementResult) },
                    nameof(OnApplyResultPrefix)
                );

                PatchMethod(
                    typeof(HealthProcessor),
                    "ApplyResult",
                    new[] { typeof(JudgementResult) },
                    nameof(OnHealthApplyResultPrefix)
                );

                var osuScoreProcessor =
                    typeof(ScoreProcessor).Assembly.GetType(
                        "osu.Game.Rulesets.Osu.Scoring.OsuScoreProcessor"
                    );

                if (osuScoreProcessor != null)
                {
                    PatchMethod(
                        osuScoreProcessor,
                        "SimulateAutoplay",
                        new[] { typeof(HitObject) },
                        nameof(OnSimulateAutoplayPrefix)
                    );
                }

                var scoreManager =
                    typeof(Score).Assembly.GetType(
                        "osu.Game.Scoring.ScoreManager"
                    );

                if (scoreManager != null)
                {
                    PatchMethod(
                        scoreManager,
                        "Submit",
                        Type.EmptyTypes,
                        nameof(OnScoreSubmitPrefix)
                    );
                }

                Toast("ModMenu loaded");
            }
            catch (Exception ex)
            {
                Toast($"Init error: {ex.Message}");
            }
        }

        private void PatchMethod(
            Type targetType,
            string methodName,
            Type[] parameters,
            string prefixMethod = null,
            string postfixMethod = null)
        {
            try
            {
                if (targetType == null)
                    return;

                var method = targetType.GetMethod(
                    methodName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static,
                    null,
                    parameters,
                    null
                );

                if (method == null)
                {
                    Toast($"Method not found: {methodName}");
                    return;
                }

                HarmonyMethod prefix = null;
                HarmonyMethod postfix = null;

                if (!string.IsNullOrEmpty(prefixMethod))
                {
                    var m = typeof(ModMenu).GetMethod(
                        prefixMethod,
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                    if (m != null)
                        prefix = new HarmonyMethod(m);
                }

                if (!string.IsNullOrEmpty(postfixMethod))
                {
                    var m = typeof(ModMenu).GetMethod(
                        postfixMethod,
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                    if (m != null)
                        postfix = new HarmonyMethod(m);
                }

                harmony.Patch(method, prefix, postfix);
            }
            catch (Exception ex)
            {
                Toast($"Patch error: {ex.Message}");
            }
        }

        public static bool OnApplyResultPrefix(
            ScoreProcessor __instance,
            JudgementResult result)
        {
            try
            {
                if (result == null)
                    return true;

                if (NoMissEnabled &&
                    result.Type == HitResult.Miss)
                {
                    ModifyJudgementResult(
                        result,
                        HitResult.Meh,
                        0
                    );
                }

                if (RelaxEnabled &&
                    result.Type != HitResult.Miss)
                {
                    ModifyJudgementResult(
                        result,
                        HitResult.Great,
                        0
                    );
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        public static bool OnHealthApplyResultPrefix(
            HealthProcessor __instance,
            JudgementResult result)
        {
            try
            {
                if (__instance == null)
                    return true;

                if (NoMissEnabled || AutoPlayEnabled)
                {
                    AddHealth(__instance, 0.15f);
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        public static bool OnSimulateAutoplayPrefix(
            object __instance,
            HitObject hitObject)
        {
            try
            {
                if (!InstantSpinEnabled || hitObject == null)
                    return true;

                var spinnerType =
                    hitObject.GetType();

                if (spinnerType.Name.Contains("Spinner"))
                {
                    var completeMethod =
                        spinnerType.GetMethod(
                            "Complete",
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance
                        );

                    completeMethod?.Invoke(hitObject, null);

                    return false;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        public static bool OnScoreSubmitPrefix()
        {
            try
            {
                if (SubmissionMode ==
                    ScoreSubmissionMode.Offline)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        private static void ModifyJudgementResult(
            JudgementResult result,
            HitResult newType,
            double offset)
        {
            try
            {
                var typeProp =
                    typeof(JudgementResult).GetProperty(
                        "Type",
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance
                    );

                typeProp?.SetValue(result, newType);

                var offsetProp =
                    typeof(JudgementResult).GetProperty(
                        "TimeOffset",
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance
                    );

                offsetProp?.SetValue(result, offset);
            }
            catch
            {
            }
        }

        private static void AddHealth(
            HealthProcessor processor,
            float amount)
        {
            try
            {
                var healthField =
                    typeof(HealthProcessor).GetField(
                        "health",
                        BindingFlags.NonPublic |
                        BindingFlags.Instance
                    );

                if (healthField == null)
                    return;

                float current =
                    (float)healthField.GetValue(processor);

                current += amount;

                if (current > 1f)
                    current = 1f;

                healthField.SetValue(processor, current);
            }
            catch
            {
            }
        }

        public void SetAutoPlay(bool enabled)
        {
            AutoPlayEnabled = enabled;

            if (enabled)
            {
                NoMissEnabled = true;
                SubmissionMode =
                    ScoreSubmissionMode.Offline;
            }

            Toast($"AutoPlay {(enabled ? "ON" : "OFF")}");
        }

        public void SetNoMiss(bool enabled)
        {
            NoMissEnabled = enabled;

            Toast($"NoMiss {(enabled ? "ON" : "OFF")}");
        }

        public void SetRelax(bool enabled)
        {
            RelaxEnabled = enabled;

            Toast($"Relax {(enabled ? "ON" : "OFF")}");
        }

        public void SetInstantSpin(bool enabled)
        {
            InstantSpinEnabled = enabled;

            Toast($"Spin {(enabled ? "ON" : "OFF")}");
        }

        public Dictionary<string, bool> GetStates()
        {
            return new Dictionary<string, bool>
            {
                { "AutoPlay", AutoPlayEnabled },
                { "NoMiss", NoMissEnabled },
                { "Relax", RelaxEnabled },
                { "InstantSpin", InstantSpinEnabled }
            };
        }

        private void Toast(string text)
        {
            try
            {
                Handler handler =
                    new Handler(context.MainLooper);

                handler.Post(() =>
                {
                    Android.Widget.Toast
                        .MakeText(
                            context,
                            text,
                            ToastLength.Short
                        )
                        ?.Show();
                });
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            try
            {
                harmony.UnpatchAll(
                    "com.osu.modmenu"
                );
            }
            catch
            {
            }
        }

        ~ModMenu()
        {
            Dispose();
        }
    }
}
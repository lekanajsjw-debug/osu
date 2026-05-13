// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;

namespace osu.Android
{
    /// <summary>
    /// Управление состоянием модов с thread-safe доступом.
    /// Моды активируются ТОЛЬКО через оверлей — игра читает состояние отсюда.
    /// </summary>
    public static class ModMenu
    {
        private static readonly object lockObject = new object();

        private static bool autoPlayEnabled;
        private static bool noMissEnabled;
        private static bool relaxEnabled;
        private static bool instantSpinEnabled;
        private static bool forceRankedEnabled;

        /// <summary>
        /// Автоматическая игра (AutoPlay мод).
        /// Когда включён — NoMiss тоже активен, Relax отключается.
        /// </summary>
        public static bool AutoPlayEnabled
        {
            get { lock (lockObject) return autoPlayEnabled; }
            private set { lock (lockObject) autoPlayEnabled = value; }
        }

        /// <summary>
        /// Невозможность проиграть (NoFail / NoMiss мод).
        /// Не может быть выключен пока активен AutoPlay.
        /// </summary>
        public static bool NoMissEnabled
        {
            get { lock (lockObject) return noMissEnabled; }
            private set { lock (lockObject) noMissEnabled = value; }
        }

        /// <summary>
        /// Расслабленный режим (Relax мод).
        /// Несовместим с AutoPlay — при включении сбрасывает его.
        /// </summary>
        public static bool RelaxEnabled
        {
            get { lock (lockObject) return relaxEnabled; }
            private set { lock (lockObject) relaxEnabled = value; }
        }

        /// <summary>
        /// Мгновенное вращение спиннеров (InstantSpin).
        /// </summary>
        public static bool InstantSpinEnabled
        {
            get { lock (lockObject) return instantSpinEnabled; }
            private set { lock (lockObject) instantSpinEnabled = value; }
        }

        /// <summary>
        /// Принудительный ranked режим (ForceRanked).
        /// </summary>
        public static bool ForceRankedEnabled
        {
            get { lock (lockObject) return forceRankedEnabled; }
            private set { lock (lockObject) forceRankedEnabled = value; }
        }

        /// <summary>
        /// Событие изменения состояния модов.
        /// ВАЖНО: стреляет из ThreadPool — подписчики обязаны
        /// маршалировать вызов на UI-поток через RunOnUiThread.
        /// </summary>
        public static event Action? OnStateChanged;

        // ── Toggle методы ──────────────────────────────────────────────────

        public static void ToggleAutoPlay()
        {
            lock (lockObject)
            {
                autoPlayEnabled = !autoPlayEnabled;

                if (autoPlayEnabled)
                {
                    // AutoPlay принудительно включает NoMiss
                    noMissEnabled = true;
                    // AutoPlay несовместим с Relax
                    relaxEnabled = false;
                }
            }

            notifyStateChanged();
        }

        public static void ToggleNoMiss()
        {
            bool changed;

            lock (lockObject)
            {
                // Нельзя выключить NoMiss пока AutoPlay активен
                if (autoPlayEnabled && noMissEnabled)
                {
                    changed = false;
                }
                else
                {
                    noMissEnabled = !noMissEnabled;
                    changed = true;
                }
            }

            if (changed)
                notifyStateChanged();
        }

        public static void ToggleRelax()
        {
            lock (lockObject)
            {
                relaxEnabled = !relaxEnabled;

                if (relaxEnabled)
                {
                    // Relax несовместим с AutoPlay
                    autoPlayEnabled = false;
                    // Сбрасываем noMiss, который мог остаться от AutoPlay
                    noMissEnabled = false;
                }
            }

            notifyStateChanged();
        }

        public static void ToggleInstantSpin()
        {
            lock (lockObject)
                instantSpinEnabled = !instantSpinEnabled;

            notifyStateChanged();
        }

        public static void ToggleForceRanked()
        {
            lock (lockObject)
                forceRankedEnabled = !forceRankedEnabled;

            notifyStateChanged();
        }

        public static void ResetAll()
        {
            lock (lockObject)
            {
                autoPlayEnabled = false;
                noMissEnabled = false;
                relaxEnabled = false;
                instantSpinEnabled = false;
                forceRankedEnabled = false;
            }

            notifyStateChanged();
        }

        // ── Отладка ────────────────────────────────────────────────────────

        public static string GetDebugInfo()
        {
            lock (lockObject)
            {
                return $"AutoPlay:{autoPlayEnabled} NoMiss:{noMissEnabled} " +
                       $"Relax:{relaxEnabled} InstantSpin:{instantSpinEnabled} " +
                       $"ForceRanked:{forceRankedEnabled}";
            }
        }

        // ── Внутреннее ────────────────────────────────────────────────────

        private static void notifyStateChanged()
        {
            // Вызываем событие в ThreadPool, чтобы не блокировать вызывающий поток.
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    OnStateChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    global::Android.Util.Log.Error("ModMenu", $"Error in OnStateChanged: {ex.Message}");
                }
            });
        }
    }
}

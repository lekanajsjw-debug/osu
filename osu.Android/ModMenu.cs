// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;

namespace osu.Android
{
    /// <summary>
    /// Управление состоянием модов с thread-safe доступом
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
        /// Автоматическая игра (AutoPlay мод)
        /// </summary>
        public static bool AutoPlayEnabled
        {
            get
            {
                lock (lockObject)
                    return autoPlayEnabled;
            }
            private set
            {
                lock (lockObject)
                    autoPlayEnabled = value;
            }
        }

        /// <summary>
        /// Невозможность проиграть (NoFail мод)
        /// </summary>
        public static bool NoMissEnabled
        {
            get
            {
                lock (lockObject)
                    return noMissEnabled;
            }
            private set
            {
                lock (lockObject)
                    noMissEnabled = value;
            }
        }

        /// <summary>
        /// Расслабленный режим (Relax мод)
        /// </summary>
        public static bool RelaxEnabled
        {
            get
            {
                lock (lockObject)
                    return relaxEnabled;
            }
            private set
            {
                lock (lockObject)
                    relaxEnabled = value;
            }
        }

        /// <summary>
        /// Мгновенное вращение спиннеров
        /// </summary>
        public static bool InstantSpinEnabled
        {
            get
            {
                lock (lockObject)
                    return instantSpinEnabled;
            }
            private set
            {
                lock (lockObject)
                    instantSpinEnabled = value;
            }
        }

        /// <summary>
        /// Принудительный ranked режим
        /// </summary>
        public static bool ForceRankedEnabled
        {
            get
            {
                lock (lockObject)
                    return forceRankedEnabled;
            }
            private set
            {
                lock (lockObject)
                    forceRankedEnabled = value;
            }
        }

        /// <summary>
        /// Событие изменения состояния модов
        /// </summary>
        public static event Action? OnStateChanged;

        /// <summary>
        /// Переключить AutoPlay мод
        /// </summary>
        public static void ToggleAutoPlay()
        {
            lock (lockObject)
            {
                autoPlayEnabled = !autoPlayEnabled;

                // Логика: AutoPlay автоматически включает NoMiss
                if (autoPlayEnabled)
                {
                    noMissEnabled = true;
                    // AutoPlay несовместим с Relax
                    relaxEnabled = false;
                }
            }

            notifyStateChanged();
        }

        /// <summary>
        /// Переключить NoMiss мод
        /// </summary>
        public static void ToggleNoMiss()
        {
            lock (lockObject)
            {
                // Если AutoPlay включен, нельзя выключить NoMiss
                if (autoPlayEnabled && noMissEnabled)
                {
                    // Игнорируем попытку выключить NoMiss при активном AutoPlay
                    return;
                }

                noMissEnabled = !noMissEnabled;
            }

            notifyStateChanged();
        }

        /// <summary>
        /// Переключить Relax мод
        /// </summary>
        public static void ToggleRelax()
        {
            lock (lockObject)
            {
                relaxEnabled = !relaxEnabled;

                // Relax несовместим с AutoPlay
                if (relaxEnabled)
                {
                    autoPlayEnabled = false;
                }
            }

            notifyStateChanged();
        }

        /// <summary>
        /// Переключить InstantSpin мод
        /// </summary>
        public static void ToggleInstantSpin()
        {
            lock (lockObject)
            {
                instantSpinEnabled = !instantSpinEnabled;
            }

            notifyStateChanged();
        }

        /// <summary>
        /// Переключить ForceRanked мод
        /// </summary>
        public static void ToggleForceRanked()
        {
            lock (lockObject)
            {
                forceRankedEnabled = !forceRankedEnabled;
            }

            notifyStateChanged();
        }

        /// <summary>
        /// Сбросить все моды
        /// </summary>
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

        /// <summary>
        /// Получить текущее состояние всех модов в виде строки для отладки
        /// </summary>
        public static string GetDebugInfo()
        {
            lock (lockObject)
            {
                return $"AutoPlay: {autoPlayEnabled}, NoMiss: {noMissEnabled}, " +
                       $"Relax: {relaxEnabled}, InstantSpin: {instantSpinEnabled}, " +
                       $"ForceRanked: {forceRankedEnabled}";
            }
        }

        private static void notifyStateChanged()
        {
            // Вызываем событие в ThreadPool, чтобы не блокировать UI поток
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

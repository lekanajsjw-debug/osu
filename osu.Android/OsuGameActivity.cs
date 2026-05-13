using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Views;
using osu.Framework.Android;
using osu.Game.Database;
using Uri = Android.Net.Uri;

namespace osu.Android
{
    [Activity(
        ConfigurationChanges = DEFAULT_CONFIG_CHANGES,
        Exported = true,
        LaunchMode = DEFAULT_LAUNCH_MODE,
        MainLauncher = true)]
    public class OsuGameActivity : AndroidGameActivity
    {
        private readonly OsuGameAndroid game;

        private bool gameCreated;

        private ModMenuOverlay? overlay;

        // Флаг: уже запрашивали разрешение, чтобы не уходить в настройки повторно
        private bool overlayPermissionRequested;

        public new bool IsTablet { get; private set; }

        public ScreenOrientation DefaultOrientation;

        public OsuGameActivity()
        {
            game = new OsuGameAndroid(this);
        }

        protected override Framework.Game CreateGame()
        {
            if (gameCreated)
                throw new InvalidOperationException("Game already created.");

            gameCreated = true;

            return game;
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            handleIntent(Intent);

            Window?.AddFlags(WindowManagerFlags.Fullscreen);
            Window?.AddFlags(WindowManagerFlags.KeepScreenOn);

            Point displaySize = new Point();

#pragma warning disable CA1422
            WindowManager?.DefaultDisplay?.GetSize(displaySize);
#pragma warning restore CA1422

            float smallestWidthDp =
                Math.Min(displaySize.X, displaySize.Y) /
                Resources.DisplayMetrics.Density;

            IsTablet = smallestWidthDp >= 600f;

            RequestedOrientation = DefaultOrientation =
                IsTablet
                    ? ScreenOrientation.FullUser
                    : ScreenOrientation.SensorLandscape;

            // Загружаем rulesets
            Assembly.Load("osu.Game.Rulesets.Osu");
            Assembly.Load("osu.Game.Rulesets.Taiko");
            Assembly.Load("osu.Game.Rulesets.Catch");
            Assembly.Load("osu.Game.Rulesets.Mania");

            // initialiseOverlay() убран отсюда — окно ещё не готово на этом этапе.
            // Инициализация происходит в OnWindowFocusChanged / OnResume.
        }

        // Вызывается когда окно полностью готово и получает фокус —
        // самый безопасный момент для AddView через WindowManager.
        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);

            if (hasFocus)
                tryInitialiseOverlay();
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Ловим возврат пользователя из экрана настроек после выдачи разрешения,
            // а также показываем overlay снова после возврата из фона.
            if (overlay != null)
                overlay.Show();
            else
                tryInitialiseOverlay();
        }

        protected override void OnPause()
        {
            // БАГ FIX: overlay висит поверх всех приложений через системный WindowManager.
            // Если не скрывать при уходе в фон — он будет виден поверх других приложений.
            overlay?.Hide();

            base.OnPause();
        }

        private void tryInitialiseOverlay()
        {
            // Уже инициализировали — ничего не делаем
            if (overlay != null)
                return;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                if (!Settings.CanDrawOverlays(this))
                {
                    // Отправляем в настройки только один раз, чтобы не зациклиться
                    if (!overlayPermissionRequested)
                    {
                        overlayPermissionRequested = true;

                        var intent = new Intent(
                            Settings.ActionManageOverlayPermission,
                            Uri.Parse("package:" + PackageName)
                        );

                        intent.AddFlags(ActivityFlags.NewTask);
                        StartActivity(intent);
                    }

                    return;
                }
            }

            overlay = new ModMenuOverlay(this);
            overlay.Show();
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            handleIntent(intent);
        }

        private void handleIntent(Intent? intent)
        {
            if (intent == null)
                return;

            switch (intent.Action)
            {
                case Intent.ActionDefault:

                    if (intent.Scheme == ContentResolver.SchemeContent)
                    {
                        if (intent.Data != null)
                            handleImportFromUris(intent.Data);
                    }

                    break;

                case Intent.ActionSend:
                case Intent.ActionSendMultiple:

                    if (intent.ClipData == null)
                        break;

                    var uris = new List<Uri>();

                    for (int i = 0; i < intent.ClipData.ItemCount; i++)
                    {
                        var item = intent.ClipData.GetItemAt(i);

                        if (item?.Uri != null)
                            uris.Add(item.Uri);
                    }

                    handleImportFromUris(uris.ToArray());

                    break;
            }
        }

        private void handleImportFromUris(params Uri[] uris)
        {
            Task.Factory.StartNew(async () =>
            {
                var tasks = new List<ImportTask>();

                await Task.WhenAll(
                    uris.Select(async uri =>
                    {
                        var task = await AndroidImportTask
                            .Create(ContentResolver, uri)
                            .ConfigureAwait(false);

                        if (task != null)
                        {
                            lock (tasks)
                                tasks.Add(task);
                        }
                    })
                ).ConfigureAwait(false);

                await game
                    .Import(tasks.ToArray())
                    .ConfigureAwait(false);

            }, TaskCreationOptions.LongRunning);
        }

        protected override void OnDestroy()
        {
            overlay?.Dispose();

            base.OnDestroy();
        }
    }
}

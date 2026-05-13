using osu.Framework.Graphics;
using osu.Framework.Threading;

namespace osu.Game.Rulesets.Catch.UI
{
    public partial class CatcherArea
    {
#if ANDROID
        private MouseInputHelper? relaxHelper;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            applyRelaxState();

            osu.Android.ModMenu.OnStateChanged += onModStateChanged;
        }

        private void onModStateChanged()
        {
            Schedule(applyRelaxState);
        }

        private void applyRelaxState()
        {
            if (osu.Android.ModMenu.RelaxEnabled)
            {
                if (relaxHelper == null)
                {
                    relaxHelper = new MouseInputHelper();
                    AddInternal(relaxHelper);
                }
            }
            else
            {
                if (relaxHelper != null)
                {
                    RemoveInternal(relaxHelper);
                    relaxHelper.Expire();
                    relaxHelper = null;
                }
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            osu.Android.ModMenu.OnStateChanged -= onModStateChanged;

            base.Dispose(isDisposing);
        }
#endif
    }
}
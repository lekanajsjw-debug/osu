using osu.Game.Rulesets.Catch.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Catch.Judgements
{
    public partial class CatchHealthProcessor : ScoreProcessor
    {
        protected override double HealthIncreaseFor(HitResult result)
        {
#if ANDROID
            if (osu.Android.ModMenu.NoMissEnabled && result == HitResult.Miss)
                return 0;
#endif

            return base.HealthIncreaseFor(result);
        }
    }
}

using osu.Game.Rulesets.Judgements;

namespace osu.Game.Rulesets.Catch.Judgements
{
    public partial class CatchHealthProcessor
    {
        public override void ApplyResult(JudgementResult result)
        {
#if ANDROID
            if (osu.Android.ModMenu.NoMissEnabled)
            {
                if (result.Type == HitResult.Miss)
                    return;
            }
#endif

            base.ApplyResult(result);
        }
    }
}
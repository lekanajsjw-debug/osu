// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

#if ANDROID
using osu.Android;
#endif

namespace osu.Game.Rulesets.Catch.Scoring
{
    public partial class CatchHealthProcessor : LegacyDrainingHealthProcessor
    {
        public CatchHealthProcessor(double drainStartTime)
            : base(drainStartTime)
        {
        }

        protected override IEnumerable<HitObject> EnumerateTopLevelHitObjects() =>
            EnumerateHitObjects(Beatmap).Where(h => h is Fruit || h is Droplet || h is Banana);

        protected override IEnumerable<HitObject> EnumerateNestedHitObjects(HitObject hitObject) =>
            Enumerable.Empty<HitObject>();

        protected override bool CheckDefaultFailCondition(JudgementResult result)
        {
#if ANDROID
            // Если NoMiss активен через оверлей — никогда не проваливаемся
            if (ModMenu.NoMissEnabled)
                return false;
#endif

            // Совпадает со стабильным поведением:
            // tiny droplet не может вызвать провал независимо от результата
            if (result.Type == HitResult.SmallTickMiss)
                return false;

            // banana shower не может вызвать провал
            if (result.HitObject is BananaShower)
                return false;

            return base.CheckDefaultFailCondition(result);
        }

        protected override double GetHealthIncreaseFor(HitObject hitObject, HitResult result)
        {
            double increase = 0;

            switch (result)
            {
                case HitResult.SmallTickMiss:
                    return 0;

                case HitResult.LargeTickMiss:
                case HitResult.Miss:
#if ANDROID
                    // Если NoMiss активен — потеря здоровья за промах = 0
                    if (ModMenu.NoMissEnabled)
                        return 0;
#endif
                    return IBeatmapDifficultyInfo.DifficultyRange(
                        Beatmap.Difficulty.DrainRate, -0.03, -0.125, -0.2);

                case HitResult.SmallTickHit:
                    increase = 0.0015;
                    break;

                case HitResult.LargeTickHit:
                    increase = 0.015;
                    break;

                case HitResult.Great:
                    increase = 0.03;
                    break;

                case HitResult.LargeBonus:
                    increase = 0.0025;
                    break;
            }

            return HpMultiplierNormal * increase;
        }
    }
}

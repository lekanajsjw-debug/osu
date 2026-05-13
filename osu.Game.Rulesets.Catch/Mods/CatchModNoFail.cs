// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Android;

namespace osu.Game.Rulesets.Catch.Mods
{
    public class CatchModNoFail : ModNoFail
    {
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ModMenuNoFailApplicator — применяет NoFail-эффект без включения мода.
    // Реализует IApplicableToHealthProcessor — передаётся в DrawableCatchRuleset.
    //
    // КАК ПОДКЛЮЧИТЬ в DrawableCatchRuleset.cs:
    //
    //   protected override HealthProcessor CreateHealthProcessor(double drainStartTime)
    //   {
    //       var processor = base.CreateHealthProcessor(drainStartTime);
    //       processor.Failed += () => !ModMenu.NoMissEnabled;
    //       return processor;
    //   }
    // ─────────────────────────────────────────────────────────────────────────
    public class ModMenuNoFailApplicator : IApplicableToHealthProcessor
    {
        public void ApplyToHealthProcessor(HealthProcessor healthProcessor)
        {
            // Подписываемся на проверку условия смерти.
            // Возвращаем false (= не умирать) когда NoMiss включён в ModMenu.
            healthProcessor.Failed += () => !ModMenu.NoMissEnabled;
        }
    }
}

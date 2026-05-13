// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch;              // CatchRuleset
using osu.Game.Rulesets.Catch.Mods;         // ModMenuAutoplayApplicator
using osu.Game.Rulesets.Catch.Objects;      // CatchHitObject
using osu.Game.Rulesets.Catch.Replays;      // CatchFramedReplayInputHandler, CatchReplayRecorder
using osu.Game.Rulesets.Mods;              // Mod
using osu.Game.Rulesets.Replays;            // ReplayInputHandler, Replay
using osu.Game.Rulesets.UI;                 // DrawableRuleset, Playfield, PassThroughInputManager
using osu.Game.Scoring;                     // Score
using osu.Android;                          // ModMenu

namespace osu.Game.Rulesets.Catch.UI
{
    public partial class DrawableCatchRuleset : DrawableRuleset<CatchHitObject>
    {
        public new IBindable<bool> HasReplayLoaded => base.HasReplayLoaded;

        public DrawableCatchRuleset(CatchRuleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods = null)
            : base(ruleset, beatmap, mods)
        {
        }

        protected override Playfield CreatePlayfield() => new CatchPlayfield(Beatmap.BeatmapInfo.Difficulty);

        protected override ReplayInputHandler CreateReplayInputHandler(Replay replay)
        {
            if (replay != null)
                return new CatchFramedReplayInputHandler(replay);

            // ModMenu AutoPlay: генерируем реплей без добавления мода в SelectedMods
            if (ModMenu.AutoPlayEnabled)
                return ModMenuAutoplayApplicator.CreateHandler(Beatmap.Beatmap);

            return null;
        }

        protected override ReplayRecorder CreateReplayRecorder(Score score)
            => new CatchReplayRecorder(score, (CatchPlayfield)Playfield);

        protected override ResumeOverlay CreateResumeOverlay()
            => new CatchResumeOverlay();

        protected override PassThroughInputManager CreateInputManager()
            => new CatchInputManager(Ruleset?.RulesetInfo);

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var catchPlayfield = (CatchPlayfield)Playfield;

            // ModMenu Relax: добавляем helper один раз — он сам проверяет флаг при каждом движении
            catchPlayfield.CatcherArea.Add(new ModMenuRelaxApplicator(catchPlayfield.CatcherArea));
        }
    }
}

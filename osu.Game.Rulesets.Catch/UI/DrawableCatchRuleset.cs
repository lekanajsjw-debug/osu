// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Mods;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Scoring;
using osuTK;
using osu.Android;

namespace osu.Game.Rulesets.Catch.UI
{
    public partial class DrawableCatchRuleset : DrawableRuleset<CatchHitObject>
    {
        public new IBindable<bool> HasReplayLoaded => base.HasReplayLoaded;

        public DrawableCatchRuleset(CatchRuleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods = null)
            : base(ruleset, beatmap, mods)
        {
        }

        protected override Playfield CreatePlayfield() => new CatchPlayfield(Beatmap.Difficulty);

        protected override ReplayInputHandler CreateReplayInputHandler(Replay replay)
        {
            // Обычный реплей из файла / со спектатора — используем как есть
            if (replay != null)
                return new CatchFramedReplayInputHandler(replay);

            // ── ModMenu AutoPlay ──────────────────────────────────────────────
            // Генерируем авто-реплей на лету, не добавляя CatchModAutoplay в SelectedMods.
            // Это значит в результате не будет пометки "Autoplay" и не упадёт ранк.
            if (ModMenu.AutoPlayEnabled)
                return ModMenuAutoplayApplicator.CreateHandler(Beatmap);
            // ─────────────────────────────────────────────────────────────────

            return null;
        }

        protected override ReplayRecorder CreateReplayRecorder(Score score)
            => new CatchReplayRecorder(score, (CatchPlayfield)Playfield);

        protected override ResumeOverlay CreateResumeOverlay()
            => new CatchResumeOverlay();

        protected override PassThroughInputManager CreateInputManager()
            => new CatchInputManager(Ruleset?.RulesetInfo);

        protected override HealthProcessor CreateHealthProcessor(double drainStartTime)
        {
            var processor = base.CreateHealthProcessor(drainStartTime);

            // ── ModMenu NoMiss ────────────────────────────────────────────────
            // Подписываемся на событие смерти. Возвращаем false (= не умирать)
            // когда NoMiss включён. Мод NoFail при этом НЕ добавляется.
            processor.Failed += () => !ModMenu.NoMissEnabled;
            // ─────────────────────────────────────────────────────────────────

            return processor;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var catchPlayfield = (CatchPlayfield)Playfield;

            // ── ModMenu Relax ─────────────────────────────────────────────────
            // Добавляем helper один раз при загрузке.
            // Он ничего не делает пока ModMenu.RelaxEnabled == false,
            // и начинает работать мгновенно как только флаг становится true —
            // не нужно перезапускать матч.
            catchPlayfield.CatcherArea.Add(new ModMenuRelaxApplicator(catchPlayfield.CatcherArea));
            // ─────────────────────────────────────────────────────────────────
        }
    }
}

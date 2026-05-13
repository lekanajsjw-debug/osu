// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Input;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osuTK;

#if ANDROID
using osu.Android;
#endif

namespace osu.Game.Rulesets.Catch.UI
{
    public partial class DrawableCatchRuleset : DrawableScrollingRuleset<CatchHitObject>
    {
        protected override bool UserScrollSpeedAdjustment => false;

        /// <summary>
        /// Список модов, которые реально применяются во время игры.
        /// Заполняется из ModMenu (оверлей) — встроенные моды игры игнорируются.
        /// </summary>
        private IReadOnlyList<Mod> activeMods = new List<Mod>();

        public DrawableCatchRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod>? mods = null)
            : base(ruleset, beatmap, mods)
        {
            Direction.Value = ScrollingDirection.Down;
            TimeRange.Value = GetTimeRange(beatmap.Difficulty.ApproachRate);
            VisualisationMethod = ScrollVisualisationMethod.Constant;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Строим список активных модов из состояния оверлея
            refreshModsFromOverlay();

            // Relax через оверлей: input идёт напрямую как позиция X,
            // клавиши лево/право не нужны — не добавляем CatchTouchInputMapper.
            bool relaxActive = false;

#if ANDROID
            relaxActive = ModMenu.RelaxEnabled;
#else
            relaxActive = activeMods.Any(m => m is ModRelax);
#endif

            if (!relaxActive)
                KeyBindingInputManager.Add(new CatchTouchInputMapper());
        }

        /// <summary>
        /// Обновляет <see cref="activeMods"/> на основе текущего состояния ModMenu.
        /// Вызывается при каждом старте игры.
        /// </summary>
        private void refreshModsFromOverlay()
        {
#if ANDROID
            var mods = new List<Mod>();

            if (ModMenu.AutoPlayEnabled)
                mods.Add(new Mods.CatchModAutoplay());

            if (ModMenu.NoMissEnabled && !ModMenu.AutoPlayEnabled)
                mods.Add(new Mods.CatchModNoFail());

            if (ModMenu.RelaxEnabled)
                mods.Add(new Mods.CatchModRelax());

            activeMods = mods;
#endif
        }

        /// <summary>
        /// Возвращает true если мод данного типа активен через оверлей.
        /// Используется в Catch-специфичных системах (Catcher, HealthProcessor и т.д.)
        /// </summary>
        public bool IsOverlayModActive<T>() where T : Mod
        {
#if ANDROID
            if (typeof(T) == typeof(ModAutoplay) || typeof(T) == typeof(Mods.CatchModAutoplay))
                return ModMenu.AutoPlayEnabled;

            if (typeof(T) == typeof(ModNoFail) || typeof(T) == typeof(Mods.CatchModNoFail))
                return ModMenu.NoMissEnabled;

            if (typeof(T) == typeof(ModRelax) || typeof(T) == typeof(Mods.CatchModRelax))
                return ModMenu.RelaxEnabled;
#endif
            return false;
        }

        protected double GetTimeRange(float approachRate) =>
            IBeatmapDifficultyInfo.DifficultyRange(approachRate, 1800, 1200, 450);

        protected override ReplayInputHandler CreateReplayInputHandler(Replay replay) =>
            new CatchFramedReplayInputHandler(replay);

        protected override ReplayRecorder CreateReplayRecorder(Score score) =>
            new CatchReplayRecorder(score, (CatchPlayfield)Playfield);

        protected override Playfield CreatePlayfield() => new CatchPlayfield(Beatmap.Difficulty);

        public override PlayfieldAdjustmentContainer CreatePlayfieldAdjustmentContainer() =>
            new CatchPlayfieldAdjustmentContainer();

        protected override PassThroughInputManager CreateInputManager() =>
            new CatchInputManager(Ruleset.RulesetInfo);

        public override DrawableHitObject<CatchHitObject>? CreateDrawableRepresentation(CatchHitObject h) => null;

        protected override ResumeOverlay CreateResumeOverlay() =>
            new DelayedResumeOverlay { Scale = new Vector2(0.65f) };
    }
}

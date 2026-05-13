// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play;
using osu.Android;

namespace osu.Game.Rulesets.Catch.UI
{
    // ВАЖНО: два варианта в зависимости от репо.
    //
    // Вариант A — файл CatchPlayer.cs в репо УЖЕ СУЩЕСТВУЕТ:
    //   Оставь только: public partial class CatchPlayer
    //   (убери ": Player" — базовый класс уже указан в другой части)
    //
    // Вариант B — файла CatchPlayer.cs в репо НЕТ:
    //   Оставь как есть: public partial class CatchPlayer : Player
    //
    public partial class CatchPlayer : Player
    {
        protected override HealthProcessor CreateHealthProcessor(double drainStartTime)
        {
            var processor = new DrainingHealthProcessor(drainStartTime, 0.05);

            // ModMenu NoMiss: не даём HP упасть до 0 пока флаг включён
            processor.Health.ValueChanged += e =>
            {
                if (ModMenu.NoMissEnabled && e.NewValue <= 0)
                    processor.Health.Value = 0.01;
            };

            return processor;
        }
    }
}

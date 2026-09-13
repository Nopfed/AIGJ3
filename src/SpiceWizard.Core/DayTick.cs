namespace SpiceWizard.Core;

/// <summary>Night resolution: everything that needs time gets one big step forward while the wizard sleeps.</summary>
public static class DayTick
{
    public static MorningReport Sleep(GameState s)
    {
        var report = new MorningReport();
        int tonight = s.Clock.Day;

        // 1. Jars and plants advance.
        s.Shelf.EndOfNight();
        s.Garden.EndOfNight();

        // 2. The cart takes the crate to town; each sauce is rated and paid for. Bottles that fill an open
        //    rush order earn double.
        foreach (var sauce in s.Crate.Sauces)
        {
            var sale = s.Town.Rate(sauce, tonight, s.Quota, s.Level, s.Rush);
            report.Sales.Add(sale);
            report.PeppercornsEarned += sale.Peppercorns;
            report.XpEarned += sale.Xp;
            if (sauce.IsBlend) s.Stats.BlendsSold++; else s.Stats.SaucesSold++;
            if (sale.Stars == 5) s.Stats.FiveStarSauces++;
        }
        s.Crate.Sauces.Clear();
        s.Peppercorns += report.PeppercornsEarned;
        s.Stats.PeppercornsEarned += report.PeppercornsEarned;

        // 2b. The rush order closes: filled (fame bonus) or lapsed on its due night (no penalty).
        if (s.Rush != null && (s.Rush.IsMet || tonight >= s.Rush.DueDay))
        {
            report.RushRecipeId = s.Rush.RecipeId;
            if (s.Rush.IsMet)
            {
                report.RushCompleted = true;
                report.RushBonusXp = Balance.RushXp;
                report.XpEarned += Balance.RushXp;
                s.Stats.RushesFilled++;
            }
            else report.RushExpired = true;
            s.Rush = null;
        }

        // 3. New day.
        s.Clock.NewDay();
        s.HastensToday = 0;
        report.Day = s.Clock.Day;

        // 4. Weekly quota is judged on the first morning of the next week.
        if (s.Clock.DayOfWeek == 1 && s.Quota != null)
        {
            report.QuotaEvaluated = true;
            report.QuotaMet = s.Quota.IsMet;
            if (report.QuotaMet)
            {
                int week = s.Quota.Week;
                report.QuotaBonusPeppercorns = Balance.QuotaBonusPeppercorns(week);
                report.QuotaBonusXp = Balance.QuotaBonusXp(week);
                report.QuotaBonusSpice = s.Rng.Pick(SpiceInfo.Rare);
                s.Peppercorns += report.QuotaBonusPeppercorns;
                s.Stats.PeppercornsEarned += report.QuotaBonusPeppercorns;
                s.Inventory.Spices[(int)report.QuotaBonusSpice.Value]++;
                report.XpEarned += report.QuotaBonusXp;
                s.Stats.QuotasMet++;
            }
        }

        // 5. Experience, levels and unlocks.
        int before = s.Level;
        report.LevelsGained = s.Progression.AddXp(report.XpEarned);
        report.NewLevel = s.Level;
        s.Spice.SetMaxForLevel(s.Level);
        s.Spice.Refill();
        if (!s.Won && s.Progression.IsMaster)
        {
            s.Won = true;
            s.WonOnDay = s.Clock.Day;
            report.BecameMaster = true;
        }

        // 6. Post next week's quota now that the level (and recipe list) is final.
        if (s.Clock.DayOfWeek == 1)
        {
            s.Quota = Quota.Generate(s.Clock.Week, s.Level, s.Rng);
            report.NewQuotaPosted = true;
        }

        // 6b. Some mornings a rush order arrives, once the wizard has a few recipes to choose from.
        if (s.Rush == null && s.Level >= Balance.RushMinLevel && s.Rng.Next(100) < Balance.RushChance)
        {
            s.Rush = RushOrder.Generate(s.Clock.Day, s.Level, s.Quota, s.Rng);
            report.RushPosted = true;
        }

        // 7. Tomorrow's sky. Rain does the morning watering.
        s.Weather = WeatherInfo.Roll(s.Clock.Day, s.Rng);
        WeatherInfo.ApplyRain(s);
        report.Weather = s.Weather;

        report.PlantsReady = s.Garden.MatureCount;
        report.JarsReady = s.Shelf.Jars.Count(j => j.IsReady);
        s.LastReport = report;
        return report;
    }
}

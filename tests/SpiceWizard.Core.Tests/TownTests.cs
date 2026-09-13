using SpiceWizard.Core;

namespace SpiceWizard.Core.Tests;

public class TownTests
{
    [Fact]
    public void Rating_pays_by_stars_and_awards_tiered_xp()
    {
        var town = new Town();
        var sale = town.Rate(new Sauce(1, 3, 1), 1, null);
        Assert.Equal(3, sale.Stars);
        Assert.Equal(15, sale.Peppercorns);
        Assert.Equal(3 * 1 * Balance.XpPerStarTier, sale.Xp);

        var big = town.Rate(new Sauce(8, 5, 1), 1, null);
        Assert.Equal(140, big.Peppercorns);
        Assert.Equal(5 * 4 * Balance.XpPerStarTier, big.Xp);
    }

    [Fact]
    public void Quota_demand_adds_a_star_and_counts_the_sale()
    {
        var town = new Town();
        var quota = new Quota { Week = 1, Lines = { new QuotaLine { RecipeId = 2, Required = 1 } } };
        var sale = town.Rate(new Sauce(2, 3, 1), 1, quota);
        Assert.Equal(4, sale.Stars);
        Assert.True(sale.OnQuota);
        Assert.True(quota.IsMet);
        Assert.Equal(1, quota.Lines[0].Sold);
        town.Rate(new Sauce(2, 3, 1), 1, quota);
        Assert.Equal(1, quota.Lines[0].Sold);   // never over-counts
    }

    [Fact]
    public void Town_gets_bored_of_the_same_sauce_within_three_days()
    {
        var town = new Town();
        Assert.Equal(3, town.Rate(new Sauce(1, 3, 1), 1, null).Stars);
        Assert.Equal(3, town.Rate(new Sauce(1, 3, 1), 1, null).Stars);
        var third = town.Rate(new Sauce(1, 3, 1), 1, null);
        Assert.Equal(2, third.Stars);
        Assert.True(third.Bored);
        Assert.Equal(2, town.Rate(new Sauce(1, 3, 3), 3, null).Stars);   // days 1..3 still count
        Assert.Equal(3, town.Rate(new Sauce(1, 3, 4), 4, null).Stars);   // day 1 sales have faded
    }

    [Fact]
    public void Craved_type_adds_a_star_all_week_but_not_to_blends()
    {
        var town = new Town();
        var quota = new Quota { Week = 1, CravedType = SauceType.Hot };
        var hot = town.Rate(new Sauce(1, 3, 1), 1, quota);
        Assert.Equal(4, hot.Stars);
        Assert.True(hot.Craved);
        var curry = town.Rate(new Sauce(2, 3, 1), 1, quota);
        Assert.Equal(3, curry.Stars);
        Assert.False(curry.Craved);
        var blend = new Blend { Peppercorns = 2 };
        Assert.False(town.Rate(new Sauce(blend, 3, 1), 1, quota).Craved);
        Assert.False(town.Rate(new Sauce(1, 3, 1), 1, null).Craved);
    }

    [Fact]
    public void Craving_is_rolled_with_the_quota_and_alternates_by_week()
    {
        var rng = new Rng(7);
        var types = new HashSet<SauceType>();
        for (int week = 1; week <= 40; week++)
        {
            var q = Quota.Generate(week, 20, rng);
            if (q.CravedType is SauceType t)
            {
                Assert.Equal(week % 2 == 1 ? SauceType.Hot : SauceType.Curry, t);
                types.Add(t);
            }
        }
        Assert.Equal(2, types.Count);
    }

    [Fact]
    public void A_forgiving_town_sits_through_one_more_repeat()
    {
        var town = new Town();
        int lv = Balance.ForgivingTownLevel;
        Assert.Equal(3, town.Rate(new Sauce(1, 3, 1), 1, null, lv).Stars);
        Assert.Equal(3, town.Rate(new Sauce(1, 3, 1), 1, null, lv).Stars);
        Assert.Equal(3, town.Rate(new Sauce(1, 3, 1), 1, null, lv).Stars);
        Assert.Equal(2, town.Rate(new Sauce(1, 3, 1), 1, null, lv).Stars);
    }

    [Fact]
    public void Stars_are_clamped()
    {
        var town = new Town();
        var quota = new Quota { Lines = { new QuotaLine { RecipeId = 1, Required = 5 } } };
        Assert.Equal(5, town.Rate(new Sauce(1, 5, 1), 1, quota).Stars);
        town.Rate(new Sauce(1, 1, 1), 1, null);
        Assert.Equal(1, town.Rate(new Sauce(1, 1, 1), 1, null).Stars);
    }

    [Fact]
    public void Quota_generation_only_uses_unlocked_recipes()
    {
        var rng = new Rng(3);
        for (int level = 1; level <= 20; level++)
        {
            var q = Quota.Generate(1, level, rng);
            Assert.InRange(q.Lines.Count, 2, 3);
            Assert.All(q.Lines, l => Assert.True(RecipeBook.Get(l.RecipeId).UnlockLevel <= level));
            Assert.Equal(q.Lines.Count, q.Lines.Select(l => l.RecipeId).Distinct().Count());
            Assert.All(q.Lines, l => Assert.InRange(l.Required, 1, 2));
        }
    }

    [Fact]
    public void Night_sells_crate_pays_levels_and_posts_weekly_quota()
    {
        var s = GameState.NewGame(11);
        s.Inventory.Sauces.Add(new Sauce(2, 5, 1));
        Actions.Ship(s, 0);
        var report = DayTick.Sleep(s);
        Assert.Equal(2, report.Day);
        Assert.Single(report.Sales);
        Assert.Equal(Balance.StartingPeppercorns + report.PeppercornsEarned, s.Peppercorns);
        Assert.Empty(s.Crate.Sauces);
        Assert.Equal(1, s.Stats.SaucesSold);
        Assert.False(report.QuotaEvaluated);

        for (int d = 2; d <= 7; d++) DayTick.Sleep(s);
        Assert.Equal(8, s.Clock.Day);
        Assert.True(s.LastReport!.QuotaEvaluated);
        Assert.True(s.LastReport.NewQuotaPosted);
        Assert.Equal(2, s.Quota!.Week);
    }

    [Fact]
    public void Meeting_the_quota_pays_a_bonus_and_a_rare_spice()
    {
        var s = GameState.NewGame(5);
        s.Quota = new Quota { Week = 1, Lines = { new QuotaLine { RecipeId = 1, Required = 1 } } };
        s.Inventory.Sauces.Add(new Sauce(1, 3, 1));
        Actions.Ship(s, 0);
        for (int d = 1; d <= 7; d++) DayTick.Sleep(s);
        var r = s.LastReport!;
        Assert.True(r.QuotaMet);
        Assert.Equal(Balance.QuotaBonusPeppercorns(1), r.QuotaBonusPeppercorns);
        Assert.NotNull(r.QuotaBonusSpice);
        Assert.Equal(Balance.StartingSpices.Length + 1, s.Inventory.Spices.Sum());
        Assert.Equal(1, s.Stats.QuotasMet);
    }

    [Fact]
    public void Reaching_level_twenty_wins_once()
    {
        var s = GameState.NewGame(1);
        s.Progression.Level = 19;
        s.Progression.Xp = Balance.XpToNext(19) - 1;
        s.Inventory.Sauces.Add(new Sauce(1, 3, 1));
        Actions.Ship(s, 0);
        var r = DayTick.Sleep(s);
        Assert.True(r.BecameMaster);
        Assert.True(s.Won);
        Assert.Equal(20, s.Level);
        Assert.Equal(0, s.Progression.AddXp(1000));
        Assert.False(DayTick.Sleep(s).BecameMaster);
    }

    [Fact]
    public void Progression_levels_and_unlock_text()
    {
        var p = new Progression();
        Assert.Equal(1, p.AddXp(25));
        Assert.Equal(2, p.Level);
        Assert.Equal(0, p.Xp);
        Assert.Equal(2, p.AddXp(Balance.XpToNext(2) + Balance.XpToNext(3) + 3));
        Assert.Equal(3, p.Xp);
        Assert.Contains("Bonnet seeds", Progression.UnlockAt(4));
        Assert.Contains("garden plot", Progression.UnlockAt(3));
        Assert.Contains("celebrates", Progression.UnlockAt(20));
        Assert.Equal("Master Spice Wizard", Progression.Title(20));
    }

    [Fact]
    public void Clock_runs_a_day_in_four_minutes()
    {
        var c = new GameClock();
        c.Advance(Balance.DayLengthSeconds / 2);
        Assert.Equal("14:00", c.TimeText());
        Assert.False(c.IsNightfall);
        c.Advance(Balance.DayLengthSeconds);
        Assert.True(c.IsNightfall);
        Assert.Equal(1.0, c.DayFraction);
        c.NewDay();
        Assert.Equal(2, c.Day);
        Assert.Equal(0.0, c.DayFraction);
        Assert.Equal(1, c.Week);
        Assert.Equal(2, c.DayOfWeek);
    }
}

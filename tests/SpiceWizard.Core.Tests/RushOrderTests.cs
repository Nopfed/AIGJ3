using SpiceWizard.Core;

namespace SpiceWizard.Core.Tests;

public class RushOrderTests
{
    static GameState AtLevel(int level, ulong seed = 7)
    {
        var s = GameState.NewGame(seed);
        s.Progression.Level = level;
        return s;
    }

    [Fact]
    public void Rush_orders_are_posted_from_level_three_and_are_due_two_nights_later()
    {
        var s = AtLevel(Balance.RushMinLevel - 1);
        for (int i = 0; i < 30; i++) { DayTick.Sleep(s); Assert.Null(s.Rush); }

        s = AtLevel(Balance.RushMinLevel);
        MorningReport? posted = null;
        while (posted == null && s.Clock.Day < 60)
        {
            var r = DayTick.Sleep(s);
            if (r.RushPosted) posted = r;
        }
        Assert.NotNull(posted);
        Assert.NotNull(s.Rush);
        Assert.Equal(s.Clock.Day + Balance.RushDays, s.Rush!.DueDay);
        Assert.True(RecipeBook.Get(s.Rush.RecipeId).UnlockLevel <= s.Level);
        Assert.InRange(s.Rush.Count, 1, 2);
        Assert.Equal("due in 2 nights", s.Rush.DueText(s.Clock.Day));
        Assert.Equal("due tonight", s.Rush.DueText(s.Rush.DueDay));
    }

    [Fact]
    public void Generate_avoids_this_weeks_quota_and_asks_for_pairs_of_cheap_sauces()
    {
        var rng = new Rng(3);
        var quota = new Quota { Week = 1, Lines = { new QuotaLine { RecipeId = 1, Required = 1 } } };
        for (int i = 0; i < 20; i++)
        {
            var rush = RushOrder.Generate(5, 2, quota, rng);   // level 2: recipes 1, 2 and 3 are unlocked
            Assert.NotEqual(1, rush.RecipeId);
            Assert.Equal(2, rush.Count);
            Assert.Equal(7, rush.DueDay);
        }
        var dear = RushOrder.Generate(5, 20, new Quota { Lines = RecipeBook.All.Where(r => r.Tier < 3).Select(r => new QuotaLine { RecipeId = r.Id, Required = 1 }).ToList() }, rng);
        Assert.Equal(1, dear.Count);
    }

    [Fact]
    public void Rush_bottles_pay_double_and_fill_the_order()
    {
        var s = AtLevel(5);
        s.Quota = new Quota { Week = 1 };   // no quota star, no craving: pay is the plain value
        s.Rush = new RushOrder { RecipeId = 2, Count = 2, DueDay = s.Clock.Day + 2 };
        s.Crate.Sauces.Add(new Sauce(2, 3, s.Clock.Day));
        s.Crate.Sauces.Add(new Sauce(1, 3, s.Clock.Day));
        var report = DayTick.Sleep(s);

        var rushed = report.Sales[0];
        Assert.True(rushed.Rush);
        Assert.Equal(16 * 2, rushed.Peppercorns);
        Assert.Contains("baker", rushed.Remark);
        Assert.False(report.Sales[1].Rush);
        Assert.Equal(15, report.Sales[1].Peppercorns);
        Assert.NotNull(s.Rush);
        Assert.Equal(1, s.Rush!.Delivered);
        Assert.False(report.RushCompleted);

        // The second bottle closes the order and pays the fame bonus; a third would be paid normally.
        s.Crate.Sauces.Add(new Sauce(2, 3, s.Clock.Day));
        s.Crate.Sauces.Add(new Sauce(2, 3, s.Clock.Day));
        report = DayTick.Sleep(s);
        Assert.True(report.Sales[0].Rush);
        Assert.False(report.Sales[1].Rush);
        Assert.True(report.RushCompleted);
        Assert.Equal(2, report.RushRecipeId);
        Assert.Equal(Balance.RushXp, report.RushBonusXp);
        Assert.Equal(report.Sales.Sum(x => x.Xp) + Balance.RushXp, report.XpEarned);
        Assert.Equal(1, s.Stats.RushesFilled);
        Assert.True(s.Rush == null || report.RushPosted);
    }

    [Fact]
    public void Blends_never_fill_a_rush_order()
    {
        var town = new Town();
        var rush = new RushOrder { RecipeId = 0, Count = 1, DueDay = 3 };
        var blend = new Blend { Peppercorns = 2 };
        var sale = town.Rate(new Sauce(blend, 3, 1), 1, null, 1, rush);
        Assert.False(sale.Rush);
        Assert.Equal(0, rush.Delivered);
    }

    [Fact]
    public void Unmet_rush_expires_on_its_due_night_without_penalty()
    {
        var s = AtLevel(5);
        int day = s.Clock.Day;
        s.Rush = new RushOrder { RecipeId = 3, Count = 1, DueDay = day + 2 };
        int pc = s.Peppercorns, xp = s.Progression.Xp;

        var r1 = DayTick.Sleep(s);                       // night of day D: still open
        Assert.False(r1.RushExpired);
        Assert.NotNull(s.Rush);
        var r2 = DayTick.Sleep(s);                       // night of D+1: still open
        Assert.False(r2.RushExpired);
        Assert.NotNull(s.Rush);
        var r3 = DayTick.Sleep(s);                       // night of D+2: lapses
        Assert.True(r3.RushExpired);
        Assert.Equal(3, r3.RushRecipeId);
        Assert.True(s.Rush == null || r3.RushPosted);
        Assert.True(s.Peppercorns >= pc);
        Assert.True(s.Progression.Xp >= xp || s.Level > 5);
    }

    [Fact]
    public void Only_one_rush_order_is_open_at_a_time()
    {
        var s = AtLevel(8, seed: 5);
        for (int i = 0; i < 80; i++)
        {
            var before = s.Rush;
            var r = DayTick.Sleep(s);
            if (before != null && !r.RushCompleted && !r.RushExpired)
            {
                Assert.False(r.RushPosted);
                Assert.Same(before, s.Rush);
            }
            if (s.Rush != null) Assert.True(s.Rush.DueDay >= s.Clock.Day);
        }
    }
}

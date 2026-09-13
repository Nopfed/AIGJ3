using SpiceWizard.Core;
using Xunit.Abstractions;

namespace SpiceWizard.Core.Tests;

public class BotPlaythroughTests
{
    readonly ITestOutputHelper _out;
    public BotPlaythroughTests(ITestOutputHelper output) { _out = output; }

    const int Seeds = 20;

    [Fact]
    public void Greedy_bot_masters_the_craft_in_a_sensible_number_of_days()
    {
        var days = new List<int>();
        for (ulong seed = 1; seed <= Seeds; seed++)
        {
            var bot = new GreedyBot(seed);
            int day = bot.Play();
            Assert.True(day > 0, $"seed {seed} never reached level 20 (level {bot.State.Level} on day {bot.State.Clock.Day})");
            days.Add(day);
            _out.WriteLine($"seed {seed}: master on day {day}, {bot.State.Stats.SaucesSold} sauces, {bot.State.Stats.BlendsSold} blends, {bot.State.Stats.PeppercornsEarned} pc earned, {bot.State.Stats.QuotasMet} quotas, {bot.State.Stats.RushesFilled} rushes");
        }
        days.Sort();
        int median = days[days.Count / 2];
        _out.WriteLine($"median {median}, min {days[0]}, max {days[^1]}");
        Assert.InRange(median, 20, 40);
        Assert.InRange(days[^1], 20, 60);
    }

    [Fact]
    public void Bot_never_goes_broke_and_always_has_a_plot_working()
    {
        var bot = new GreedyBot(42, maxDays: 40);
        int brokeDays = 0;
        while (!bot.State.Won && bot.State.Clock.Day <= 40)
        {
            typeof(GreedyBot).GetMethod("PlayDay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(bot, null);
            bool canAffordAnything = bot.State.Peppercorns >= Species.Get(PepperSpecies.Bell).SeedCost
                || bot.State.Garden.Plots.Any(p => !p.IsEmpty)
                || bot.State.Inventory.Seeds.Sum() > 0
                || bot.State.Crate.Sauces.Count > 0;
            if (!canAffordAnything) brokeDays++;
            DayTick.Sleep(bot.State);
        }
        Assert.Equal(0, brokeDays);
    }
}

using SpiceWizard.Core;

namespace SpiceWizard.Core.Tests;

public class ActionsTests
{
    static GameState Fresh() => GameState.NewGame(7);

    [Fact]
    public void New_game_starts_with_seeds_peppercorns_and_a_quota()
    {
        var s = Fresh();
        Assert.Equal(Balance.StartingPeppercorns, s.Peppercorns);
        Assert.Equal(Balance.StartingBellSeeds, s.Inventory.Seed(PepperSpecies.Bell));
        Assert.NotNull(s.Quota);
        Assert.All(s.Quota!.Lines, l => Assert.True(RecipeBook.Get(l.RecipeId).UnlockLevel <= 1));
        Assert.Equal(1, s.Clock.Day);
        Assert.Equal("06:00", s.Clock.TimeText());
    }

    [Fact]
    public void Planting_watering_and_harvesting_a_bell()
    {
        var s = Fresh();
        Assert.True(Actions.PlantSeed(s, 0, PepperSpecies.Bell).Ok);
        Assert.False(Actions.PlantSeed(s, 0, PepperSpecies.Bell).Ok);
        Assert.False(Actions.PlantSeed(s, 7, PepperSpecies.Bell).Ok);   // locked plot
        Assert.False(Actions.PlantSeed(s, 1, PepperSpecies.Ghost).Ok);  // no seeds
        Assert.Equal(3, s.Inventory.Seed(PepperSpecies.Bell));

        Assert.True(Actions.Water(s, 0).Ok);
        Assert.False(Actions.Water(s, 0).Ok);
        Assert.Equal(Garden.BucketCapacity - 1, s.Garden.BucketWater);
        Assert.False(Actions.Harvest(s, 0).Ok);

        for (int i = 0; i < 2; i++) { DayTick.Sleep(s); Actions.Water(s, 0); }
        DayTick.Sleep(s);
        Assert.True(s.Garden.Plots[0].Plant!.IsMature);
        Assert.True(Actions.Harvest(s, 0).Ok);
        Assert.Equal(3, s.Inventory.Pepper(PepperSpecies.Bell));
        Assert.True(s.Garden.Plots[0].IsEmpty);
    }

    [Fact]
    public void Bucket_empties_and_refills()
    {
        var s = Fresh();
        for (int i = 0; i < 4; i++) Actions.PlantSeed(s, i, PepperSpecies.Bell);
        for (int i = 0; i < 4; i++) Assert.True(Actions.Water(s, i).Ok);
        Assert.Equal(0, s.Garden.BucketWater);
        Assert.False(Actions.RefillBucket(s).Ok == false);
        Assert.Equal(Garden.BucketCapacity, s.Garden.BucketWater);
        Assert.False(Actions.RefillBucket(s).Ok);
    }

    [Fact]
    public void Pep_talk_costs_spice_and_is_once_per_day()
    {
        var s = Fresh();
        Actions.PlantSeed(s, 0, PepperSpecies.Bell);
        int before = s.Spice.Current;
        Assert.True(Actions.PepTalk(s, 0).Ok);
        Assert.Equal(before - Balance.PepTalkSpiceCost, s.Spice.Current);
        Assert.False(Actions.PepTalk(s, 0).Ok);
        s.Spice.Current = 0;
        DayTick.Sleep(s);
        Assert.Equal(s.Spice.Max, s.Spice.Current);
    }

    [Fact]
    public void Hasten_spell_is_once_a_day_and_skips_two_nights()
    {
        var s = Fresh();
        Actions.PlantSeed(s, 0, PepperSpecies.Bell);
        Actions.PlantSeed(s, 1, PepperSpecies.Bell);
        s.Inventory.Peppers[(int)PepperSpecies.Bell] = 2;
        Actions.FillJar(s, 0, PepperSpecies.Bell);

        int before = s.Spice.Current;
        Assert.True(Actions.HastenPlant(s, 0).Ok);
        Assert.Equal(before - Balance.HastenSpiceCost, s.Spice.Current);
        Assert.Equal(Balance.HastenNights, s.Garden.Plots[0].Plant!.Points);
        Assert.Equal(PlantStage.Budding, s.Garden.Plots[0].Plant!.Stage);
        Assert.False(Actions.HastenPlant(s, 1).Ok);   // spent for today
        Assert.False(Actions.HastenJar(s, 0).Ok);

        DayTick.Sleep(s);
        Assert.False(s.HastenedToday);
        s.Spice.Current = Balance.HastenSpiceCost - 1;
        Assert.False(Actions.HastenJar(s, 0).Ok);     // too little spice
        s.Spice.Current = Balance.HastenSpiceCost;
        Assert.True(Actions.HastenJar(s, 0).Ok);      // 1 night + 2 = ready, not yet aged
        Assert.True(s.Shelf.Jars[0].IsReady);
        Assert.False(s.Shelf.Jars[0].IsAged);

        DayTick.Sleep(s);
        Assert.True(s.Shelf.Jars[0].IsAged);
        Assert.False(Actions.HastenJar(s, 0).Ok);     // already aged
        Assert.True(Actions.HastenPlant(s, 0).Ok);    // caps at maturity
        Assert.True(s.Garden.Plots[0].Plant!.IsMature);
        Assert.False(Actions.HastenPlant(s, 0).Ok);
    }

    [Fact]
    public void Eating_peppers_restores_spice_by_heat()
    {
        var s = Fresh();
        s.Inventory.Peppers[(int)PepperSpecies.Ghost] = 1;
        s.Inventory.Peppers[(int)PepperSpecies.Bell] = 1;
        Assert.False(Actions.Eat(s, PepperSpecies.Bell).Ok);   // already full
        s.Spice.Current = 1;
        Assert.True(Actions.Eat(s, PepperSpecies.Ghost).Ok);
        Assert.Equal(6, s.Spice.Current);
        Assert.False(Actions.Eat(s, PepperSpecies.Ghost).Ok);  // none left
    }

    [Fact]
    public void Fermenting_turns_two_peppers_into_mash()
    {
        var s = Fresh();
        s.Inventory.Peppers[(int)PepperSpecies.Bell] = 3;
        Assert.False(Actions.FillJar(s, 2, PepperSpecies.Bell).Ok);   // locked jar
        Assert.True(Actions.FillJar(s, 0, PepperSpecies.Bell).Ok);
        Assert.Equal(1, s.Inventory.Pepper(PepperSpecies.Bell));
        Assert.False(Actions.FillJar(s, 1, PepperSpecies.Bell).Ok);   // not enough
        Assert.False(Actions.EmptyJar(s, 0).Ok);
        DayTick.Sleep(s); DayTick.Sleep(s);
        Assert.True(Actions.EmptyJar(s, 0).Ok);
        Assert.Equal(1, s.Inventory.Mash[(int)PepperSpecies.Bell]);
        Assert.True(s.Shelf.Jars[0].IsEmpty);

        Actions.FillJar(s, 0, PepperSpecies.Bell);
        s.Inventory.Peppers[(int)PepperSpecies.Bell] = 2;
        Actions.FillJar(s, 0, PepperSpecies.Bell);
        for (int i = 0; i < 4; i++) DayTick.Sleep(s);
        Actions.EmptyJar(s, 0);
        Assert.Equal(1, s.Inventory.AgedMash[(int)PepperSpecies.Bell]);
    }

    [Fact]
    public void Grinding_makes_powder_for_one_spice()
    {
        var s = Fresh();
        s.Inventory.Peppers[(int)PepperSpecies.Banana] = 1;
        Assert.True(Actions.Grind(s, PepperSpecies.Banana).Ok);
        Assert.Equal(1, s.Inventory.PowderOf(PepperSpecies.Banana));
        Assert.Equal(Balance.BaseSpiceMax - 1, s.Spice.Current);
        Assert.False(Actions.Grind(s, PepperSpecies.Banana).Ok);
    }

    [Fact]
    public void Cooking_checks_ingredients_level_and_spice()
    {
        var s = Fresh();
        Assert.Contains("Missing", Actions.CookBlocker(s, RecipeBook.Get(1), false));
        s.Inventory.Mash[(int)PepperSpecies.Bell] = 1;
        Assert.Equal("", Actions.CookBlocker(s, RecipeBook.Get(1), false));
        Assert.Contains("level", Actions.CookBlocker(s, RecipeBook.Get(7), false));

        s.Spice.Current = 2;
        Assert.False(Actions.Cook(s, 1, false).Ok);
        s.Spice.Current = 3;
        int pc = s.Peppercorns;
        Assert.True(Actions.Cook(s, 1, true).Ok);
        Assert.Equal(pc - 2, s.Peppercorns);       // recipe peppercorn + extra
        Assert.Equal(0, s.Spice.Current);
        Assert.Single(s.Inventory.Sauces);
        // level 1 is "still learning" (-1), extra peppercorn (+1): back to 3
        Assert.Equal(3, s.Inventory.Sauces[0].Quality);
    }

    [Fact]
    public void Aged_mash_is_used_first_and_raises_quality()
    {
        var s = Fresh();
        s.Progression.Level = 3;
        s.Inventory.Mash[(int)PepperSpecies.Bell] = 1;
        s.Inventory.AgedMash[(int)PepperSpecies.Bell] = 1;
        Assert.True(Actions.Cook(s, 1, false).Ok);
        Assert.Equal(0, s.Inventory.AgedMash[(int)PepperSpecies.Bell]);
        Assert.Equal(1, s.Inventory.Mash[(int)PepperSpecies.Bell]);
        Assert.Equal(4, s.Inventory.Sauces[0].Quality);
    }

    [Theory]
    [InlineData(1, false, false, 2)]
    [InlineData(3, false, false, 3)]
    [InlineData(7, false, false, 4)]
    [InlineData(7, true, true, 5)]
    [InlineData(1, true, true, 4)]
    public void Quality_formula(int level, bool aged, bool extra, int expected)
    {
        Assert.Equal(expected, Actions.Quality(RecipeBook.Get(1), level, aged, extra));
    }

    [Fact]
    public void Market_respects_level_and_wallet()
    {
        var s = Fresh();
        Assert.False(Actions.BuySeed(s, PepperSpecies.Ghost).Ok);
        Assert.True(Actions.BuySeed(s, PepperSpecies.Banana).Ok);
        Assert.Equal(Balance.StartingPeppercorns - 8, s.Peppercorns);
        Assert.False(Actions.BuySpice(s, Spice.Cloves).Ok);
        Assert.True(Actions.BuySpice(s, Spice.Cumin).Ok);
        s.Peppercorns = 0;
        Assert.False(Actions.BuySpice(s, Spice.Cumin).Ok);
    }

    [Fact]
    public void Shipping_moves_sauces_between_inventory_and_crate()
    {
        var s = Fresh();
        for (int i = 0; i < 7; i++) s.Inventory.Sauces.Add(new Sauce(1, 3, 1));
        for (int i = 0; i < 6; i++) Assert.True(Actions.Ship(s, 0).Ok);
        Assert.False(Actions.Ship(s, 0).Ok);
        Assert.True(s.Crate.IsFull);
        Assert.True(Actions.Unship(s, 0).Ok);
        Assert.Equal(2, s.Inventory.Sauces.Count);
    }

    [Fact]
    public void Save_round_trips_the_whole_state()
    {
        var s = Fresh();
        Actions.PlantSeed(s, 0, PepperSpecies.Bell);
        Actions.Water(s, 0);
        s.Inventory.Peppers[(int)PepperSpecies.Banana] = 2;
        Actions.FillJar(s, 0, PepperSpecies.Banana);
        s.Inventory.Sauces.Add(new Sauce(2, 4, 1));
        Actions.Ship(s, 0);
        DayTick.Sleep(s);

        string json = SaveSystem.ToJson(s);
        var back = SaveSystem.FromJson(json)!;
        Assert.Equal(s.Clock.Day, back.Clock.Day);
        Assert.Equal(s.Peppercorns, back.Peppercorns);
        Assert.Equal(PepperSpecies.Banana, back.Shelf.Jars[0].Species);
        Assert.True(back.Garden.Plots[0].Plant!.WateredYesterday);
        Assert.Equal(s.Progression.Xp, back.Progression.Xp);
        Assert.Equal(s.Rng.State, back.Rng.State);
        Assert.Equal(s.Quota!.Lines.Count, back.Quota!.Lines.Count);
        Assert.NotNull(back.LastReport);
        Assert.Single(back.LastReport!.Sales);
        Assert.Null(SaveSystem.FromJson("nonsense"));
        Assert.Null(SaveSystem.FromJson("99|{}"));
    }
}

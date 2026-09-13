using SpiceWizard.Core;

namespace SpiceWizard.Core.Tests;

public class ProgressionTests
{
    [Fact]
    public void Every_level_from_two_to_twenty_unlocks_something()
    {
        for (int level = 2; level <= Balance.MaxLevel; level++)
            Assert.False(string.IsNullOrEmpty(Progression.UnlockAt(level)), $"level {level} unlocks nothing");
    }

    [Fact]
    public void Late_levers_show_up_in_the_unlock_text()
    {
        Assert.Contains("crate", Progression.UnlockAt(Balance.BigCrateLevel));
        Assert.Contains("hasten", Progression.UnlockAt(Balance.SecondHastenLevel));
        Assert.Contains("ages", Progression.UnlockAt(Balance.QuickAgeLevel));
        Assert.Contains("forgives", Progression.UnlockAt(Balance.ForgivingTownLevel));
        Assert.Contains("Rainbow Chutney", Progression.UnlockAt(15));
        Assert.Contains("Wizard's Curry", Progression.UnlockAt(17));
        Assert.Contains("fermenting jar", Progression.UnlockAt(11));
    }

    [Fact]
    public void Recipe_ids_are_dense_and_tiers_follow_the_value_bands()
    {
        for (int i = 0; i < RecipeBook.All.Length; i++) Assert.Equal(i + 1, RecipeBook.All[i].Id);
        Assert.Equal(5, RecipeBook.Get(9).Tier);
        Assert.Equal(5, RecipeBook.Get(10).Tier);
        Assert.True(RecipeBook.All.Zip(RecipeBook.All.Skip(1)).All(p => p.First.UnlockLevel <= p.Second.UnlockLevel), "unlock levels are not sorted");
    }

    [Fact]
    public void Crate_grows_at_the_big_crate_level()
    {
        var s = GameState.NewGame(3);
        for (int i = 0; i < Balance.BigCrateCapacity + 1; i++) s.Inventory.Sauces.Add(new Sauce(1, 3, 1));
        for (int i = 0; i < Balance.CrateCapacity; i++) Assert.True(Actions.Ship(s, 0).Ok);
        Assert.False(Actions.Ship(s, 0).Ok);
        s.Progression.Level = Balance.BigCrateLevel;
        Assert.Equal(Balance.BigCrateCapacity, s.CrateCapacity);
        for (int i = Balance.CrateCapacity; i < Balance.BigCrateCapacity; i++) Assert.True(Actions.Ship(s, 0).Ok);
        Assert.False(Actions.Ship(s, 0).Ok);
        Assert.True(s.Crate.IsFull(s.Level));
    }

    [Fact]
    public void Second_hasten_cast_arrives_at_its_level()
    {
        var s = GameState.NewGame(3);
        s.Progression.Level = Balance.SecondHastenLevel;
        s.Spice.SetMaxForLevel(s.Level);
        s.Spice.Refill();
        Actions.PlantSeed(s, 0, PepperSpecies.Bell);
        Actions.PlantSeed(s, 1, PepperSpecies.Bell);
        Actions.PlantSeed(s, 2, PepperSpecies.Bell);
        Assert.Equal(2, s.HastensLeft);
        Assert.True(Actions.HastenPlant(s, 0).Ok);
        Assert.True(Actions.HastenPlant(s, 1).Ok);
        Assert.Equal(0, s.HastensLeft);
        Assert.Contains("spent", Actions.HastenPlant(s, 2).Message);
        DayTick.Sleep(s);
        Assert.Equal(2, s.HastensLeft);
    }

    [Fact]
    public void Mash_ages_a_night_sooner_from_the_quick_age_level_and_jars_keep_their_own_timer()
    {
        var s = GameState.NewGame(3);
        s.Inventory.Peppers[(int)PepperSpecies.Bell] = 4;
        Assert.True(Actions.FillJar(s, 0, PepperSpecies.Bell).Ok);
        Assert.Equal(Balance.NightsToAge, s.Shelf.Jars[0].AgeNights);
        s.Progression.Level = Balance.QuickAgeLevel;
        Assert.True(Actions.FillJar(s, 1, PepperSpecies.Bell).Ok);
        Assert.Equal(Balance.NightsToAge - 1, s.Shelf.Jars[1].AgeNights);
        for (int n = 0; n < Balance.NightsToAge - 1; n++) DayTick.Sleep(s);
        Assert.False(s.Shelf.Jars[0].IsAged);
        Assert.True(s.Shelf.Jars[1].IsAged);
        Assert.Contains("Aged in 1 more", s.Shelf.Jars[0].Status());
    }
}

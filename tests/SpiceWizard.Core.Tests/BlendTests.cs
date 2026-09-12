using SpiceWizard.Core;

namespace SpiceWizard.Core.Tests;

public class BlendTests
{
    static GameState Fresh(int level = Balance.BlendUnlockLevel)
    {
        var s = GameState.NewGame(7);
        s.Progression.Level = level;
        return s;
    }

    static Blend BonnetMasala()
    {
        var b = new Blend { Peppercorns = 1 };
        b.Powder[(int)PepperSpecies.Bonnet] = 1;
        b.Spices[(int)Spice.Cumin] = 1;
        b.Spices[(int)Spice.Coriander] = 1;
        b.Spices[(int)Spice.Cloves] = 1;
        return b;
    }

    [Fact]
    public void Worth_value_tier_and_heat_follow_the_pinches()
    {
        var b = BonnetMasala();
        Assert.Equal(5, b.Pinches);
        Assert.Equal(3, b.Heat);
        // Bonnet powder 2 + 2*3 = 8, cumin 3, coriander 3, cloves 6, peppercorn 2.
        Assert.Equal(22, b.Worth);
        Assert.Equal((int)Math.Round(22 * Balance.BlendValueMultiplier), b.Value);
        Assert.Equal(2, b.Tier);
        Assert.Equal(1, new Blend { Spices = { [(int)Spice.Cumin] = 2 } }.Tier);
    }

    [Fact]
    public void Quality_rules_stack_from_a_plain_two_stars()
    {
        var b = BonnetMasala();
        Assert.Equal(5, b.Quality);   // kick, aromatic, peppercorn
        b.Peppercorns = 0;
        Assert.Equal(4, b.Quality);
        b.Spices[(int)Spice.Cloves] = 0;
        Assert.Equal(3, b.Quality);   // heat only
        b.Powder[(int)PepperSpecies.Bonnet] = 0;
        Assert.Equal(1, b.Quality);   // no pepper at all
        var mild = new Blend { Powder = { [(int)PepperSpecies.Bell] = 1 }, Spices = { [(int)Spice.Cumin] = 1 } };
        Assert.Equal(Balance.BlendBaseQuality, mild.Quality);
        Assert.Empty(mild.Notes());
    }

    [Fact]
    public void Names_come_from_the_hottest_powder_and_the_mix_and_stay_short()
    {
        Assert.Equal("Bonnet Masala", BonnetMasala().Name);
        var rub = new Blend { Peppercorns = 1, Powder = { [(int)PepperSpecies.Bell] = 1, [(int)PepperSpecies.Ghost] = 1 } };
        Assert.Equal("Ghost Rub", rub.Name);
        Assert.Equal("Banana Blend", new Blend { Powder = { [(int)PepperSpecies.Banana] = 1 }, Spices = { [(int)Spice.Ginger] = 1 } }.Name);
        Assert.Equal("Bell Dust", new Blend { Powder = { [(int)PepperSpecies.Bell] = 2 } }.Name);
        Assert.Equal("Spice Blend", new Blend { Spices = { [(int)Spice.Cumin] = 1, [(int)Spice.Ginger] = 1 } }.Name);
        foreach (var sp in Species.All)
            foreach (var kind in new[] { "Masala", "Blend", "Dust", "Rub" })
                Assert.True((sp.Name + " " + kind).Length <= 14);
    }

    [Fact]
    public void Same_pinches_same_key_regardless_of_order_added()
    {
        var a = new Blend();
        a.Adjust(Ingredient.Of(Spice.Cumin), 1);
        a.Adjust(Ingredient.Powder(PepperSpecies.Bell), 1);
        var b = new Blend();
        b.Adjust(Ingredient.Powder(PepperSpecies.Bell), 1);
        b.Adjust(Ingredient.Of(Spice.Cumin), 1);
        Assert.Equal(a.Key, b.Key);
        b.Adjust(Ingredient.Peppercorns(1), 1);
        Assert.NotEqual(a.Key, b.Key);
        b.Adjust(Ingredient.Peppercorns(1), -1);
        b.Adjust(Ingredient.Peppercorns(1), -1);   // never below zero
        Assert.Equal(a.Key, b.Key);
        b.Adjust(Ingredient.Mash(PepperSpecies.Bell), 1);   // mash is not pinchable
        Assert.Equal(a.Key, b.Key);
    }

    [Fact]
    public void Making_a_blend_checks_level_pinches_spice_and_pantry()
    {
        var s = Fresh(level: 1);
        var b = BonnetMasala();
        Assert.Contains("level", Actions.BlendBlocker(s, b));
        s.Progression.Level = Balance.BlendUnlockLevel;
        Assert.Contains("Missing Bonnet powder", Actions.BlendBlocker(s, b));

        s.Inventory.Powder[(int)PepperSpecies.Bonnet] = 1;
        s.Inventory.Spices[(int)Spice.Cumin] = 1;
        s.Inventory.Spices[(int)Spice.Coriander] = 1;
        s.Inventory.Spices[(int)Spice.Cloves] = 1;
        Assert.Equal("", Actions.BlendBlocker(s, b));

        Assert.Contains("at least", Actions.BlendBlocker(s, new Blend { Peppercorns = 1 }));
        Assert.Contains("At most", Actions.BlendBlocker(s, new Blend { Peppercorns = Balance.MaxBlendPinches + 1 }));
        s.Peppercorns = 0;
        Assert.Contains("peppercorns", Actions.BlendBlocker(s, b));
        s.Peppercorns = 5;
        s.Spice.Current = Balance.BlendSpiceCost - 1;
        Assert.False(Actions.MakeBlend(s, b).Ok);

        s.Spice.Current = Balance.BlendSpiceCost;
        var r = Actions.MakeBlend(s, b);
        Assert.True(r.Ok, r.Message);
        Assert.Equal(0, s.Spice.Current);
        Assert.Equal(4, s.Peppercorns);
        Assert.Equal(0, s.Inventory.PowderOf(PepperSpecies.Bonnet));
        Assert.Equal(0, s.Inventory.SpiceOf(Spice.Cloves));
        var made = Assert.Single(s.Inventory.Sauces);
        Assert.True(made.IsBlend);
        Assert.Equal(0, made.RecipeId);
        Assert.Equal("Bonnet Masala", made.Name);
        Assert.Equal(5, made.Quality);
        Assert.Equal(5, b.Pinches);   // the draft survives to be mixed again
        Assert.NotSame(b, made.Blend);
    }

    [Fact]
    public void Town_loves_a_new_blend_once_then_tires_of_it_like_a_sauce()
    {
        var town = new Town();
        var b = BonnetMasala();
        b.Peppercorns = 0;   // 4 stars on its own
        var first = town.Rate(new Sauce(b, b.Quality, 1), 1, null);
        Assert.True(first.Novel);
        Assert.Equal(5, first.Stars);
        Assert.Equal((int)Math.Round(b.Value * Balance.StarMultiplier[5]), first.Peppercorns);
        Assert.Equal(5 * b.Tier * Balance.XpPerStarTier, first.Xp);
        Assert.Contains("new flavour", first.Remark);

        var second = town.Rate(new Sauce(b.Clone(), b.Quality, 1), 1, null);
        Assert.False(second.Novel);
        Assert.Equal(4, second.Stars);
        var third = town.Rate(new Sauce(b.Clone(), b.Quality, 2), 2, null);
        Assert.True(third.Bored);
        Assert.Equal(3, third.Stars);
        Assert.Contains("Bonnet Masala again", third.Remark);

        // A different blend is new again, and blends never bore the town of a recipe sauce.
        var other = b.Clone();
        other.Peppercorns = 1;
        Assert.True(town.Rate(new Sauce(other, other.Quality, 2), 2, null).Novel);
        Assert.Equal(0, town.RecentSales(1, 2));
    }

    [Fact]
    public void Blends_never_count_toward_the_quota()
    {
        var quota = new Quota { Week = 1, Lines = { new QuotaLine { RecipeId = 1, Required = 1 } } };
        var town = new Town();
        var b = BonnetMasala();
        var sale = town.Rate(new Sauce(b, b.Quality, 1), 1, quota);
        Assert.False(sale.OnQuota);
        Assert.Equal(0, quota.Lines[0].Sold);
    }

    [Fact]
    public void Blends_ship_sell_and_save_like_sauces()
    {
        var s = Fresh();
        var b = BonnetMasala();
        s.Inventory.Sauces.Add(new Sauce(b, b.Quality, 1));
        Assert.True(Actions.Ship(s, 0).Ok);
        string json = SaveSystem.ToJson(s);
        var back = SaveSystem.FromJson(json)!;
        var shipped = Assert.Single(back.Crate.Sauces);
        Assert.True(shipped.IsBlend);
        Assert.Equal(b.Key, shipped.Blend!.Key);
        Assert.Equal("Bonnet Masala", shipped.Name);

        var report = DayTick.Sleep(back);
        var sale = Assert.Single(report.Sales);
        Assert.True(sale.Novel);
        Assert.Equal(1, back.Stats.BlendsSold);
        Assert.Equal(0, back.Stats.SaucesSold);
        Assert.Contains(b.Key, back.Town.TastedBlends);
        Assert.True(back.Town.HasTasted(b));

        var again = SaveSystem.FromJson(SaveSystem.ToJson(back))!;
        Assert.Contains(b.Key, again.Town.TastedBlends);
        Assert.Equal(b.Key, again.Town.Sales[0].BlendKey);
    }

    [Fact]
    public void Level_two_announces_blending()
    {
        Assert.Contains("blend", Progression.UnlockAt(Balance.BlendUnlockLevel));
    }
}

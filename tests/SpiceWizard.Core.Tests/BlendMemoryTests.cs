using SpiceWizard.Core;

namespace SpiceWizard.Core.Tests;

/// <summary>The town's memory of tasted blends and the weekly favourite it asks for again.</summary>
public class BlendMemoryTests
{
    /// <summary>Kick, aroma and a peppercorn: five stars before the town has its say.</summary>
    static Blend BonnetMasala()
    {
        var b = new Blend { Peppercorns = 1 };
        b.Powder[(int)PepperSpecies.Bonnet] = 1;
        b.Spices[(int)Spice.Cumin] = 1;
        b.Spices[(int)Spice.Coriander] = 1;
        b.Spices[(int)Spice.Cloves] = 1;
        return b;
    }

    /// <summary>Kick and a peppercorn: four stars, so a novelty star still shows.</summary>
    static Blend BonnetRub()
    {
        var b = new Blend { Peppercorns = 1 };
        b.Powder[(int)PepperSpecies.Bonnet] = 1;
        return b;
    }

    [Fact]
    public void A_key_rebuilds_the_same_blend()
    {
        var b = BonnetMasala();
        var back = Blend.FromKey(b.Key);
        Assert.NotNull(back);
        Assert.Equal(b.Key, back!.Key);
        Assert.Equal(b.Name, back.Name);
        Assert.Null(Blend.FromKey("not a key"));
        Assert.Null(Blend.FromKey("1,2|3|4"));
    }

    [Fact]
    public void The_town_remembers_a_blend_at_its_best()
    {
        var town = new Town();
        var b = BonnetRub();
        Assert.Equal(4, b.Quality);
        var first = town.Rate(new Sauce(b, b.Quality, 3), 3, null);
        Assert.Equal(5, first.Stars);
        var m = Assert.Single(town.Memories);
        Assert.Equal(b.Key, m.Key);
        Assert.Equal(b.Name, m.Name);
        Assert.Equal(first.Stars, m.Stars);
        Assert.Equal(first.Peppercorns, m.Pay);
        Assert.Equal(3, m.Day);

        // A later, duller sale (no novelty star) does not lower the record.
        var again = town.Rate(new Sauce(b, b.Quality, 9), 9, null);
        Assert.True(again.Stars < first.Stars);
        Assert.Single(town.Memories);
        Assert.Equal(first.Stars, town.Memories[0].Stars);
        Assert.Equal(3, town.Memories[0].Day);
    }

    [Fact]
    public void Old_saves_backfill_memories_from_the_keys()
    {
        var b = BonnetRub();
        var town = new Town { TastedBlends = { b.Key, "garbage" } };
        town.BackfillMemories();
        var m = Assert.Single(town.Memories);
        Assert.Equal(b.Name, m.Name);
        Assert.Equal(b.Quality + 1, m.Stars);
        town.BackfillMemories();
        Assert.Single(town.Memories);   // idempotent
    }

    [Fact]
    public void Favourite_blend_earns_a_star_and_extra_pay_once()
    {
        var town = new Town();
        var b = BonnetMasala();
        town.TastedBlends.Add(b.Key);   // no novelty star in play
        var quota = new Quota { Week = 2, FavouriteKey = b.Key, FavouriteName = b.Name };
        var plain = new Town { TastedBlends = { b.Key } }.Rate(new Sauce(b, 3, 8), 8, null);
        var sale = town.Rate(new Sauce(b, 3, 8), 8, quota);
        Assert.True(sale.Favourite);
        Assert.Equal(plain.Stars + 1, sale.Stars);
        int expected = (int)Math.Round(Math.Round(b.Value * Balance.StarMultiplier[sale.Stars]) * Balance.FavouriteBlendMultiplier);
        Assert.Equal(expected, sale.Peppercorns);
        Assert.True(quota.FavouriteSold);
        Assert.True(quota.IsMet);   // the favourite never holds the weekly bonus back

        var second = town.Rate(new Sauce(b, 3, 8), 8, quota);
        Assert.False(second.Favourite);
        Assert.Equal(plain.Stars, second.Stars);
    }

    [Fact]
    public void The_favourite_is_never_a_bored_sale()
    {
        var town = new Town();
        var b = BonnetMasala();
        town.Rate(new Sauce(b, 3, 5), 5, null);
        town.Rate(new Sauce(b, 3, 5), 5, null);
        Assert.True(town.Rate(new Sauce(b, 3, 5), 5, null).Bored);
        var quota = new Quota { Week = 1, FavouriteKey = b.Key };
        var fav = town.Rate(new Sauce(b, 3, 5), 5, quota);
        Assert.False(fav.Bored);
        Assert.Equal(4, fav.Stars);
    }

    [Fact]
    public void Quota_asks_for_a_favourite_only_when_the_town_loved_a_blend()
    {
        var rng = new Rng(3);
        var town = new Town();
        Assert.Null(Quota.Generate(1, 5, rng, town).FavouriteKey);
        Assert.Null(Quota.Generate(1, 5, rng).FavouriteKey);

        var dull = new Blend { Peppercorns = 2 };   // 2 base + 1 peppercorn + 1 novelty - 1 no pepper = 3 stars
        town.Rate(new Sauce(dull, dull.Quality, 1), 1, null);
        Assert.Null(Quota.Generate(2, 5, rng, town).FavouriteKey);

        var b = BonnetMasala();   // 5 stars on first taste
        town.Rate(new Sauce(b, b.Quality, 2), 2, null);
        var q = Quota.Generate(3, 5, rng, town);
        Assert.Equal(b.Key, q.FavouriteKey);
        Assert.Equal(b.Name, q.FavouriteName);
        Assert.True(q.WantsBlend(b.Key));
        Assert.False(q.WantsBlend(dull.Key));
    }

    [Fact]
    public void Tired_recipes_are_those_the_town_would_mark_down_tonight()
    {
        var town = new Town();
        int lv = 5;
        Assert.Empty(town.TiredOf(4, lv));
        town.Rate(new Sauce(1, 3, 3), 3, null, lv);
        town.Rate(new Sauce(1, 3, 3), 3, null, lv);
        var tired = Assert.Single(town.TiredOf(4, lv));
        Assert.Equal(1, tired.Id);
        Assert.Empty(town.TiredOf(3 + Balance.BoredomWindowDays, lv));
    }

    [Fact]
    public void Memories_and_favourite_survive_a_save()
    {
        var s = GameState.NewGame(1);
        var b = BonnetMasala();
        s.Town.Rate(new Sauce(b, b.Quality, 1), 1, null);
        s.Quota!.FavouriteKey = b.Key;
        s.Quota.FavouriteName = b.Name;
        var back = SaveSystem.FromJson(SaveSystem.ToJson(s))!;
        var m = Assert.Single(back.Town.Memories);
        Assert.Equal(b.Name, m.Name);
        Assert.Equal(b.Key, back.Quota!.FavouriteKey);
        Assert.Equal(b.Key, m.Blend!.Key);
    }
}

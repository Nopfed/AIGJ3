using System.Text.Json.Serialization;

namespace SpiceWizard.Core;

public sealed class SaleRecord
{
    public int Day { get; set; }
    public int RecipeId { get; set; }
    /// <summary>Set for blends (RecipeId 0): the blend's <see cref="Blend.Key"/>.</summary>
    public string? BlendKey { get; set; }
}

public sealed class SaleResult
{
    public Sauce Sauce { get; set; } = new();
    public int Stars { get; set; }
    public int Peppercorns { get; set; }
    public int Xp { get; set; }
    public string Remark { get; set; } = "";
    public bool OnQuota { get; set; }
    public bool Bored { get; set; }
    /// <summary>A blend the town had never tasted before.</summary>
    public bool Novel { get; set; }
    /// <summary>The sauce is of the type the town craves this week.</summary>
    public bool Craved { get; set; }
    /// <summary>The sauce filled an open rush order and was paid at the rush rate.</summary>
    public bool Rush { get; set; }
    /// <summary>The blend is this week's town favourite and was paid the favourite bonus.</summary>
    public bool Favourite { get; set; }
}

/// <summary>
/// What the town remembers of a blend it has tasted: enough to list it on the notice board and to ask for
/// it again as a favourite. <see cref="Stars"/> and <see cref="Pay"/> are the best the blend has ever done.
/// </summary>
public sealed class BlendMemory
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public int Stars { get; set; }
    public int Pay { get; set; }
    /// <summary>The day the town first tasted it.</summary>
    public int Day { get; set; }

    [JsonIgnore] public Blend? Blend => Core.Blend.FromKey(Key);
}

/// <summary>
/// A rush order: someone in town wants a few bottles of one sauce within two days and pays double for
/// them. Only one is open at a time; an unmet order simply lapses on its due night.
/// </summary>
public sealed class RushOrder
{
    public int RecipeId { get; set; }
    public int Count { get; set; }
    public int Delivered { get; set; }
    /// <summary>The cart on the night of this day is the last one that counts.</summary>
    public int DueDay { get; set; }
    public int PayMultiplier { get; set; } = Balance.RushPayMultiplier;

    public bool IsMet => Delivered >= Count;
    public bool Wants(int recipeId) => !IsMet && recipeId == RecipeId;
    public int DaysLeft(int day) => DueDay - day;

    /// <summary>"due tonight" / "due tomorrow night" / "due in N nights".</summary>
    public string DueText(int day) => DaysLeft(day) switch
    {
        <= 0 => "due tonight",
        1 => "due tomorrow night",
        int n => "due in " + n + " nights",
    };

    /// <summary>
    /// Rolls an order from the recipes the wizard can cook, avoiding this week's quota so the two pull in
    /// different directions. Cheap sauces are wanted in pairs, dear ones singly.
    /// </summary>
    public static RushOrder Generate(int day, int level, Quota? quota, Rng rng)
    {
        var pool = RecipeBook.UnlockedAt(level).Where(r => quota == null || !quota.Wants(r.Id)).ToList();
        if (pool.Count == 0) pool = RecipeBook.UnlockedAt(level).ToList();
        var r = rng.Pick(pool);
        return new RushOrder { RecipeId = r.Id, Count = r.Tier >= 3 ? 1 : 2, DueDay = day + Balance.RushDays };
    }
}

public sealed class ShippingCrate
{
    public List<Sauce> Sauces { get; set; } = new();
    public bool IsFull(int level) => Sauces.Count >= Balance.CrateCapacityAt(level);
}

/// <summary>
/// The town: it rates sauces, pays in peppercorns, and gets bored of repeats. It also loves trying a
/// blend it has never tasted, but blends never count toward the notice-board quota.
/// </summary>
public sealed class Town
{
    public List<SaleRecord> Sales { get; set; } = new();
    /// <summary>Keys of every blend the town has tasted; the first taste of a new one earns a star.</summary>
    public List<string> TastedBlends { get; set; } = new();
    /// <summary>One entry per tasted blend, in tasting order. See <see cref="BlendMemory"/>.</summary>
    public List<BlendMemory> Memories { get; set; } = new();

    public int RecentSales(int recipeId, int day) =>
        Sales.Count(s => s.RecipeId == recipeId && s.BlendKey == null && s.Day > day - Balance.BoredomWindowDays);

    public int RecentBlendSales(string key, int day) =>
        Sales.Count(s => s.BlendKey == key && s.Day > day - Balance.BoredomWindowDays);

    public bool HasTasted(Blend blend) => TastedBlends.Contains(blend.Key);

    public BlendMemory? MemoryOf(string key) => Memories.FirstOrDefault(m => m.Key == key);

    /// <summary>Tasted blends, best first, then most recent first.</summary>
    public IEnumerable<BlendMemory> BestBlends() => Memories.OrderByDescending(m => m.Stars).ThenByDescending(m => m.Pay).ThenByDescending(m => m.Day);

    /// <summary>The town's best-loved blends: any that ever earned <see cref="Balance.FavouriteBlendStars"/> stars.</summary>
    public IEnumerable<BlendMemory> Favourites() => Memories.Where(m => m.Stars >= Balance.FavouriteBlendStars);

    /// <summary>
    /// Older saves only kept the keys; this fills in a memory for each so the tasted page is never empty.
    /// The stars and pay are what a first taste of that blend would have earned.
    /// </summary>
    public void BackfillMemories()
    {
        foreach (var key in TastedBlends)
        {
            if (MemoryOf(key) != null || Blend.FromKey(key) is not Blend blend) continue;
            int stars = Math.Clamp(blend.Quality + 1, 1, 5);
            Memories.Add(new BlendMemory { Key = key, Name = blend.Name, Stars = stars, Pay = (int)Math.Round(blend.Value * Balance.StarMultiplier[stars]) });
        }
    }

    /// <summary>Recipes the town would tire of if another bottle arrived on the cart tonight.</summary>
    public IEnumerable<Recipe> TiredOf(int day, int level) =>
        RecipeBook.UnlockedAt(level).Where(r => RecentSales(r.Id, day) >= Balance.BoredomThresholdAt(level));

    /// <summary>
    /// Rates one sauce or blend delivered on the night of <paramref name="day"/> and records the sale.
    /// <paramref name="level"/> is the wizard's level, which sets how many repeats the town forgives.
    /// A bottle that fills an open <paramref name="rush"/> order is paid at the rush rate.
    /// </summary>
    public SaleResult Rate(Sauce sauce, int day, Quota? quota, int level = 1, RushOrder? rush = null)
    {
        var blend = sauce.Blend;
        bool onQuota = blend == null && quota != null && quota.Wants(sauce.RecipeId);
        bool favourite = blend != null && quota != null && quota.WantsBlend(blend.Key);
        bool rushed = blend == null && rush != null && rush.Wants(sauce.RecipeId);
        bool craved = blend == null && quota?.CravedType != null && quota.CravedType == sauce.Recipe!.Type;
        bool novel = blend != null && !HasTasted(blend);
        // The favourite jar is treated like a quota line: an extra star, and the town does not tire of it.
        bool bored = !favourite && (blend == null ? RecentSales(sauce.RecipeId, day) : RecentBlendSales(blend.Key, day)) >= Balance.BoredomThresholdAt(level);
        int stars = Math.Clamp(sauce.Quality + (onQuota || favourite ? 1 : 0) + (craved ? 1 : 0) + (novel ? 1 : 0) - (bored ? 1 : 0), 1, 5);
        int pay = (int)Math.Round(sauce.BaseValue * Balance.StarMultiplier[stars]);
        if (rushed) pay *= rush!.PayMultiplier;
        if (favourite) pay = (int)Math.Round(pay * Balance.FavouriteBlendMultiplier);
        int xp = stars * sauce.Tier * Balance.XpPerStarTier;

        Sales.Add(new SaleRecord { Day = day, RecipeId = sauce.RecipeId, BlendKey = blend?.Key });
        if (novel) TastedBlends.Add(blend!.Key);
        if (blend != null) Remember(blend, stars, pay, day);
        if (blend == null) quota?.RecordSale(sauce.RecipeId);
        if (favourite) quota!.FavouriteSold = true;
        if (rushed) rush!.Delivered++;

        return new SaleResult
        {
            Sauce = sauce,
            Stars = stars,
            Peppercorns = pay,
            Xp = xp,
            OnQuota = onQuota,
            Bored = bored,
            Novel = novel,
            Craved = craved,
            Rush = rushed,
            Favourite = favourite,
            Remark = Remark(stars, bored, onQuota, novel, craved, rushed, favourite, sauce.Name),
        };
    }

    /// <summary>Writes the blend into the town's memory, or raises its best stars and pay.</summary>
    void Remember(Blend blend, int stars, int pay, int day)
    {
        var m = MemoryOf(blend.Key);
        if (m == null) { Memories.Add(new BlendMemory { Key = blend.Key, Name = blend.Name, Stars = stars, Pay = pay, Day = day }); return; }
        if (stars > m.Stars || (stars == m.Stars && pay > m.Pay)) { m.Stars = stars; m.Pay = pay; }
    }

    static string Remark(int stars, bool bored, bool onQuota, bool novel, bool craved, bool rushed, bool favourite, string name)
    {
        if (bored && stars < 5) return "\"" + name + " again? We have had our fill.\"";
        if (rushed) return "\"The baker ran the whole way. Double pay, as promised.\"";
        if (favourite) return "\"Our favourite! The council paid extra for the jar.\"";
        if (onQuota && stars >= 4) return "\"Just what the notice asked for. Splendid!\"";
        if (novel && stars >= 4) return "\"A new flavour! Everyone wants a pinch.\"";
        if (craved && stars >= 4) return "\"Exactly what we were craving. More!\"";
        return stars switch
        {
            5 => "\"The whole square is talking about it!\"",
            4 => "\"Rich, warm and properly fiery.\"",
            3 => "\"A fair sauce. It did the job.\"",
            2 => "\"Bit thin. The baker fed it to his cat.\"",
            _ => "\"The blacksmith used it to strip paint.\"",
        };
    }
}

public sealed class QuotaLine
{
    public int RecipeId { get; set; }
    public int Required { get; set; }
    public int Sold { get; set; }
    public bool IsMet => Sold >= Required;
}

public sealed class Quota
{
    public int Week { get; set; }
    public List<QuotaLine> Lines { get; set; } = new();
    /// <summary>Some weeks the town craves hot sauces or curries: every bottle of that type earns a star.</summary>
    public SauceType? CravedType { get; set; }
    /// <summary>
    /// A blend the town loved and asks to taste again this week (its <see cref="Blend.Key"/>). One jar earns
    /// the quota star and <see cref="Balance.FavouriteBlendMultiplier"/> pay; it is a treat on top of the
    /// lines, never needed to meet them.
    /// </summary>
    public string? FavouriteKey { get; set; }
    public string? FavouriteName { get; set; }
    public bool FavouriteSold { get; set; }
    public bool IsMet => Lines.All(l => l.IsMet);

    public bool WantsBlend(string key) => FavouriteKey != null && !FavouriteSold && FavouriteKey == key;

    public static string CravingText(SauceType type) => "The town craves " + (type == SauceType.Hot ? "hot sauces" : "curries") + " this week.";

    public bool Wants(int recipeId) => Lines.Any(l => l.RecipeId == recipeId);

    public void RecordSale(int recipeId)
    {
        var line = Lines.FirstOrDefault(l => l.RecipeId == recipeId);
        if (line != null && line.Sold < line.Required) line.Sold++;
    }

    /// <summary>
    /// Two or three lines drawn from recipes the wizard can already cook; higher tiers ask for fewer bottles.
    /// When the <paramref name="town"/> has a favourite blend, one of them is asked for as well.
    /// </summary>
    public static Quota Generate(int week, int level, Rng rng, Town? town = null)
    {
        var pool = RecipeBook.UnlockedAt(level).ToList();
        int lineCount = Math.Min(pool.Count, level >= 4 ? 3 : 2);
        var quota = new Quota { Week = week };
        while (quota.Lines.Count < lineCount)
        {
            var r = rng.Pick(pool);
            pool.Remove(r);
            int required = r.Tier >= 3 ? 1 : rng.Range(1, 2);
            quota.Lines.Add(new QuotaLine { RecipeId = r.Id, Required = required });
        }
        quota.Lines.Sort((a, b) => a.RecipeId.CompareTo(b.RecipeId));
        // The craving alternates between the two types so neither is favoured over a long game.
        if (rng.Next(100) < Balance.CravingChance) quota.CravedType = week % 2 == 1 ? SauceType.Hot : SauceType.Curry;
        var favourites = town?.Favourites().ToList();
        if (favourites is { Count: > 0 })
        {
            var f = rng.Pick(favourites);
            quota.FavouriteKey = f.Key;
            quota.FavouriteName = f.Name;
        }
        return quota;
    }
}

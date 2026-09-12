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
}

public sealed class ShippingCrate
{
    public List<Sauce> Sauces { get; set; } = new();
    public bool IsFull => Sauces.Count >= Balance.CrateCapacity;
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

    public int RecentSales(int recipeId, int day) =>
        Sales.Count(s => s.RecipeId == recipeId && s.BlendKey == null && s.Day > day - Balance.BoredomWindowDays);

    public int RecentBlendSales(string key, int day) =>
        Sales.Count(s => s.BlendKey == key && s.Day > day - Balance.BoredomWindowDays);

    public bool HasTasted(Blend blend) => TastedBlends.Contains(blend.Key);

    /// <summary>Rates one sauce or blend delivered on the night of <paramref name="day"/> and records the sale.</summary>
    public SaleResult Rate(Sauce sauce, int day, Quota? quota)
    {
        var blend = sauce.Blend;
        bool onQuota = blend == null && quota != null && quota.Wants(sauce.RecipeId);
        bool novel = blend != null && !HasTasted(blend);
        bool bored = (blend == null ? RecentSales(sauce.RecipeId, day) : RecentBlendSales(blend.Key, day)) >= Balance.BoredomThreshold;
        int stars = Math.Clamp(sauce.Quality + (onQuota ? 1 : 0) + (novel ? 1 : 0) - (bored ? 1 : 0), 1, 5);
        int pay = (int)Math.Round(sauce.BaseValue * Balance.StarMultiplier[stars]);
        int xp = stars * sauce.Tier * Balance.XpPerStarTier;

        Sales.Add(new SaleRecord { Day = day, RecipeId = sauce.RecipeId, BlendKey = blend?.Key });
        if (novel) TastedBlends.Add(blend!.Key);
        if (blend == null) quota?.RecordSale(sauce.RecipeId);

        return new SaleResult
        {
            Sauce = sauce,
            Stars = stars,
            Peppercorns = pay,
            Xp = xp,
            OnQuota = onQuota,
            Bored = bored,
            Novel = novel,
            Remark = Remark(stars, bored, onQuota, novel, sauce.Name),
        };
    }

    static string Remark(int stars, bool bored, bool onQuota, bool novel, string name)
    {
        if (bored && stars < 5) return "\"" + name + " again? We have had our fill.\"";
        if (onQuota && stars >= 4) return "\"Just what the notice asked for. Splendid!\"";
        if (novel && stars >= 4) return "\"A new flavour! Everyone wants a pinch.\"";
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
    public bool IsMet => Lines.All(l => l.IsMet);

    public bool Wants(int recipeId) => Lines.Any(l => l.RecipeId == recipeId);

    public void RecordSale(int recipeId)
    {
        var line = Lines.FirstOrDefault(l => l.RecipeId == recipeId);
        if (line != null && line.Sold < line.Required) line.Sold++;
    }

    /// <summary>Two or three lines drawn from recipes the wizard can already cook; higher tiers ask for fewer bottles.</summary>
    public static Quota Generate(int week, int level, Rng rng)
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
        return quota;
    }
}

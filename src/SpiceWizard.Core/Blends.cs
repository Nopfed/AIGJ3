using System.Text.Json.Serialization;

namespace SpiceWizard.Core;

/// <summary>One line of the star breakdown shown at the mortar: what the rule is and what it does.</summary>
public readonly record struct QualityNote(string Text, int Delta);

/// <summary>
/// A spice blend of the wizard's own design: 2–5 pinches of pepper powder, merchant spices and peppercorns
/// mixed at the mortar. Stored as counts per ingredient, so two blends with the same pinches are the same
/// blend as far as the town is concerned (<see cref="Key"/>).
/// </summary>
public sealed class Blend
{
    public int[] Powder { get; set; } = new int[Inventory.SpeciesCount];
    public int[] Spices { get; set; } = new int[Inventory.SpiceCount];
    public int Peppercorns { get; set; }

    [JsonIgnore] public int Pinches => Powder.Sum() + Spices.Sum() + Peppercorns;
    [JsonIgnore] public bool HasPowder => Powder.Any(n => n > 0);
    [JsonIgnore] public int DistinctSpices => Spices.Count(n => n > 0);
    [JsonIgnore] public int Heat => Enumerable.Range(0, Inventory.SpeciesCount).Sum(i => Powder[i] * Species.All[i].Heat);

    /// <summary>What the pinches would cost at the merchant, before the blending premium.</summary>
    [JsonIgnore]
    public int Worth =>
        Enumerable.Range(0, Inventory.SpeciesCount).Sum(i => Powder[i] * PowderWorth((PepperSpecies)i))
        + Enumerable.Range(0, Inventory.SpiceCount).Sum(i => Spices[i] * SpiceInfo.Prices[i])
        + Peppercorns * Balance.PeppercornPinchWorth;

    [JsonIgnore] public int Value => (int)Math.Round(Worth * Balance.BlendValueMultiplier);

    /// <summary>Tier by value on the recipe book's scale (tier 1 sells for 12–16, tier 4 for 55–70); it drives XP.</summary>
    [JsonIgnore] public int Tier => Value < 20 ? 1 : Value < 32 ? 2 : Value < 55 ? 3 : 4;

    /// <summary>Same pinches, same key. Used for novelty and boredom in town.</summary>
    [JsonIgnore] public string Key => string.Join(",", Powder) + "|" + string.Join(",", Spices) + "|" + Peppercorns;

    /// <summary>Rebuilds a blend from its <see cref="Key"/>; null if the text is not one.</summary>
    public static Blend? FromKey(string key)
    {
        var parts = key.Split('|');
        if (parts.Length != 3) return null;
        var powder = parts[0].Split(',');
        var spices = parts[1].Split(',');
        if (powder.Length != Inventory.SpeciesCount || spices.Length != Inventory.SpiceCount) return null;
        var b = new Blend();
        for (int i = 0; i < powder.Length; i++) if (!int.TryParse(powder[i], out b.Powder[i])) return null;
        for (int i = 0; i < spices.Length; i++) if (!int.TryParse(spices[i], out b.Spices[i])) return null;
        if (!int.TryParse(parts[2], out int pc)) return null;
        b.Peppercorns = pc;
        return b;
    }

    [JsonIgnore] public int Quality => Math.Clamp(Balance.BlendBaseQuality + Notes().Sum(n => n.Delta), 1, 5);

    /// <summary>The hottest powder in the mix, which names and colours the blend.</summary>
    [JsonIgnore]
    public PepperSpecies? Dominant
    {
        get
        {
            for (int i = Inventory.SpeciesCount - 1; i >= 0; i--)
                if (Powder[i] > 0) return (PepperSpecies)i;
            return null;
        }
    }

    /// <summary>"Bonnet Masala", "Ghost Rub", "Bell Dust"... never longer than 14 characters so it fits the crate rows.</summary>
    [JsonIgnore]
    public string Name
    {
        get
        {
            string pepper = Dominant is { } d ? Species.NameOf(d) : "Spice";
            string kind = DistinctSpices >= 3 ? "Masala" : Peppercorns > 0 ? "Rub" : DistinctSpices > 0 ? "Blend" : "Dust";
            return pepper + " " + kind;
        }
    }

    [JsonIgnore]
    public IEnumerable<Ingredient> Ingredients
    {
        get
        {
            for (int i = 0; i < Inventory.SpeciesCount; i++) if (Powder[i] > 0) yield return Ingredient.Powder((PepperSpecies)i, Powder[i]);
            for (int i = 0; i < Inventory.SpiceCount; i++) if (Spices[i] > 0) yield return Ingredient.Of((Spice)i, Spices[i]);
            if (Peppercorns > 0) yield return Ingredient.Peppercorns(Peppercorns);
        }
    }

    public static int PowderWorth(PepperSpecies s) => Balance.PowderWorthBase + Balance.PowderWorthPerHeat * Species.Get(s).Heat;

    /// <summary>Every star adjustment this blend earns on top of <see cref="Balance.BlendBaseQuality"/>.</summary>
    public IEnumerable<QualityNote> Notes()
    {
        if (!HasPowder) yield return new QualityNote("No pepper in it", -1);
        if (Heat >= Balance.BlendKickHeat) yield return new QualityNote("Heat " + Heat + ": a real kick", 1);
        if (DistinctSpices >= Balance.BlendAromaticSpices) yield return new QualityNote("Three spices: aromatic", 1);
        if (Peppercorns > 0) yield return new QualityNote("Cracked peppercorn", 1);
    }

    public int CountOf(Ingredient ing) => ing.Kind switch
    {
        IngredientKind.Powder => Powder[ing.Index],
        IngredientKind.Spice => Spices[ing.Index],
        IngredientKind.Peppercorn => Peppercorns,
        _ => 0,
    };

    /// <summary>Adds or removes pinches; peppers and mash are not pinchable and are ignored.</summary>
    public void Adjust(Ingredient ing, int delta)
    {
        switch (ing.Kind)
        {
            case IngredientKind.Powder: Powder[ing.Index] = Math.Max(0, Powder[ing.Index] + delta); break;
            case IngredientKind.Spice: Spices[ing.Index] = Math.Max(0, Spices[ing.Index] + delta); break;
            case IngredientKind.Peppercorn: Peppercorns = Math.Max(0, Peppercorns + delta); break;
        }
    }

    public void Clear()
    {
        Array.Clear(Powder);
        Array.Clear(Spices);
        Peppercorns = 0;
    }

    public Blend Clone() => new() { Powder = (int[])Powder.Clone(), Spices = (int[])Spices.Clone(), Peppercorns = Peppercorns };
}

namespace SpiceWizard.Core;

public sealed record SpeciesInfo(
    PepperSpecies Species,
    string Name,
    int GrowthPoints,
    int Yield,
    int Heat,
    int SeedCost,
    int UnlockLevel,
    string CareHint);

public static class Species
{
    public static readonly SpeciesInfo[] All =
    {
        new(PepperSpecies.Bell,   "Bell",   3, 3, 1,  5, 1, "Water daily. Ignores pep talks."),
        new(PepperSpecies.Banana, "Banana", 4, 2, 2,  8, 1, "Water every other day. Loves pep talks."),
        new(PepperSpecies.Bonnet, "Bonnet", 6, 2, 3, 12, 4, "Water and pep talk daily, or it sulks."),
        new(PepperSpecies.Ghost,  "Ghost",  4, 1, 5, 20, 8, "Water every other day only. Pep talks scare it."),
    };

    public static SpeciesInfo Get(PepperSpecies s) => All[(int)s];
    public static string NameOf(PepperSpecies s) => All[(int)s].Name;
}

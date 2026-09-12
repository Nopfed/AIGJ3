namespace SpiceWizard.Core;

public enum PlantStage { Seed, Sprout, Budding, Mature }

public sealed class Plant
{
    public PepperSpecies Species { get; set; }
    public int Points { get; set; }
    public bool WateredToday { get; set; }
    public bool WateredYesterday { get; set; }
    public bool PepTalkedToday { get; set; }
    public int Age { get; set; }

    public Plant() { }
    public Plant(PepperSpecies species) { Species = species; }

    public SpeciesInfo Info => Core.Species.Get(Species);
    public bool IsMature => Points >= Info.GrowthPoints;

    public PlantStage Stage
    {
        get
        {
            if (IsMature) return PlantStage.Mature;
            if (Points == 0) return PlantStage.Seed;
            return Points * 2 < Info.GrowthPoints ? PlantStage.Sprout : PlantStage.Budding;
        }
    }

    /// <summary>Growth points this plant earns tonight given today's care. Each species has its own temperament.</summary>
    public int GrowthTonight()
    {
        switch (Species)
        {
            case PepperSpecies.Bell:
                return WateredToday ? 1 : 0;
            case PepperSpecies.Banana:
            {
                int w = (WateredToday || WateredYesterday) ? 1 : 0;
                return w == 0 ? 0 : w + (PepTalkedToday ? 1 : 0);
            }
            case PepperSpecies.Bonnet:
                if (!PepTalkedToday) return 0;
                return 1 + (WateredToday ? 1 : 0);
            case PepperSpecies.Ghost:
                if (PepTalkedToday) return 0;
                return (WateredToday ^ WateredYesterday) ? 1 : 0;
            default:
                return 0;
        }
    }

    /// <summary>One line for the UI explaining whether it will grow tonight and why.</summary>
    public string Mood()
    {
        if (IsMature) return "Ready to harvest!";
        int g = GrowthTonight();
        switch (Species)
        {
            case PepperSpecies.Bell:
                return WateredToday ? "Content. Will grow tonight." : "Thirsty. Needs water.";
            case PepperSpecies.Banana:
                if (g == 0) return "Thirsty. Needs water.";
                return PepTalkedToday ? "Beaming! Double growth tonight." : "Happy. A pep talk would help.";
            case PepperSpecies.Bonnet:
                if (!PepTalkedToday) return "Sulking. Wants a pep talk.";
                return WateredToday ? "Adored. Growing fast." : "Flattered but thirsty.";
            case PepperSpecies.Ghost:
                if (PepTalkedToday) return "Spooked! Hiding tonight.";
                if (WateredToday && WateredYesterday) return "Drowning. Too much water.";
                return g > 0 ? "Drifting calmly. Will grow." : "Parched. Needs water.";
        }
        return "";
    }

    /// <summary>Grows as if that many well-tended nights had passed.</summary>
    public void Hasten(int nights) => Points = Math.Min(Info.GrowthPoints, Points + nights);

    public void EndOfNight()
    {
        Points = Math.Min(Info.GrowthPoints, Points + GrowthTonight());
        WateredYesterday = WateredToday;
        WateredToday = false;
        PepTalkedToday = false;
        Age++;
    }
}

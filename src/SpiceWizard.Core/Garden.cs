namespace SpiceWizard.Core;

public sealed class Plot
{
    public Plant? Plant { get; set; }
    public bool IsEmpty => Plant == null;
}

public sealed class Garden
{
    public const int MaxPlots = 8;
    public const int BucketCapacity = 4;

    public Plot[] Plots { get; set; } = Enumerable.Range(0, MaxPlots).Select(_ => new Plot()).ToArray();
    public int BucketWater { get; set; } = BucketCapacity;

    public static int UnlockedPlots(int level)
    {
        int n = 4;
        if (level >= 3) n++;
        if (level >= 6) n++;
        if (level >= 9) n++;
        if (level >= 12) n++;
        return n;
    }

    public void EndOfNight()
    {
        foreach (var plot in Plots)
            plot.Plant?.EndOfNight();
    }

    public int MatureCount => Plots.Count(p => p.Plant != null && p.Plant.IsMature);
}

public sealed class Jar
{
    public const int PeppersPerJar = 2;
    public const int NightsToFerment = 2;
    public const int NightsToAge = 4;

    public PepperSpecies? Species { get; set; }
    public int Nights { get; set; }

    public bool IsEmpty => Species == null;
    public bool IsReady => !IsEmpty && Nights >= NightsToFerment;
    public bool IsAged => !IsEmpty && Nights >= NightsToAge;

    public string Status()
    {
        if (IsEmpty) return "Empty jar";
        string name = Core.Species.NameOf(Species!.Value);
        if (IsAged) return $"Aged {name} mash";
        if (IsReady) return $"{name} mash ready. Aged in {NightsToAge - Nights} more";
        return $"{name} fermenting. {NightsToFerment - Nights} night{(NightsToFerment - Nights == 1 ? "" : "s")} left";
    }
}

public sealed class FermentShelf
{
    public const int MaxJars = 4;

    public Jar[] Jars { get; set; } = Enumerable.Range(0, MaxJars).Select(_ => new Jar()).ToArray();

    public static int UnlockedJars(int level)
    {
        int n = 2;
        if (level >= 5) n++;
        if (level >= 10) n++;
        return n;
    }

    public void EndOfNight()
    {
        foreach (var jar in Jars)
            if (!jar.IsEmpty) jar.Nights++;
    }
}

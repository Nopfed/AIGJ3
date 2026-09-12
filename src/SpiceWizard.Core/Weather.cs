namespace SpiceWizard.Core;

public enum Weather { Clear, Windy, Rain }

/// <summary>The sky for the day, rolled each dawn. Wind is for show; rain does the wizard's watering for him.</summary>
public static class WeatherInfo
{
    public static string Name(Weather w) => w switch
    {
        Weather.Windy => "Windy",
        Weather.Rain => "Rain",
        _ => "Clear",
    };

    /// <summary>One line for the morning report and the HUD tooltip.</summary>
    public static string Describe(Weather w) => w switch
    {
        Weather.Windy => "A blustery day. Hold on to your hat.",
        Weather.Rain => "Rain! The garden waters itself and the bucket stays full.",
        _ => "Clear skies.",
    };

    /// <summary>Rolls the weather for <paramref name="day"/>. The first days are always clear so the watering
    /// lesson lands before the sky starts helping.</summary>
    public static Weather Roll(int day, Rng rng)
    {
        if (day <= Balance.ClearDaysAtStart) return Weather.Clear;
        int r = rng.Next(100);
        if (r < Balance.RainChance) return Weather.Rain;
        if (r < Balance.RainChance + Balance.WindyChance) return Weather.Windy;
        return Weather.Clear;
    }

    /// <summary>Rain soaks every plant and fills the bucket. Called at dawn and again whenever a seed goes in.</summary>
    public static void ApplyRain(GameState s)
    {
        if (s.Weather != Weather.Rain) return;
        foreach (var plot in s.Garden.Plots)
            if (plot.Plant != null && !plot.Plant.IsMature) plot.Plant.WateredToday = true;
        s.Garden.BucketWater = Garden.BucketCapacity;
    }
}

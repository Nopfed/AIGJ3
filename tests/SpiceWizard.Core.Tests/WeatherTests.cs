using SpiceWizard.Core;
using Xunit;

namespace SpiceWizard.Core.Tests;

public class WeatherTests
{
    [Fact]
    public void OpeningDaysAreClear()
    {
        var s = GameState.NewGame(5);
        Assert.Equal(Weather.Clear, s.Weather);
        for (int i = 0; i < Balance.ClearDaysAtStart - 1; i++)
        {
            DayTick.Sleep(s);
            Assert.Equal(Weather.Clear, s.Weather);
        }
    }

    [Fact]
    public void RollProducesEveryKindOfSky()
    {
        var rng = new Rng(77);
        var seen = new HashSet<Weather>();
        for (int i = 0; i < 200; i++) seen.Add(WeatherInfo.Roll(10, rng));
        Assert.Equal(3, seen.Count);
    }

    [Fact]
    public void RainWatersTheGardenAndFillsTheBucket()
    {
        var s = GameState.NewGame(1);
        s.Garden.Plots[0].Plant = new Plant(PepperSpecies.Bell);
        s.Garden.Plots[1].Plant = new Plant(PepperSpecies.Ghost);
        s.Garden.Plots[2].Plant = new Plant(PepperSpecies.Bell) { Points = 99 };
        s.Garden.BucketWater = 0;
        s.Weather = Weather.Rain;
        WeatherInfo.ApplyRain(s);
        Assert.True(s.Garden.Plots[0].Plant!.WateredToday);
        Assert.True(s.Garden.Plots[1].Plant!.WateredToday);
        Assert.False(s.Garden.Plots[2].Plant!.WateredToday); // ripe plants no longer drink
        Assert.Equal(Garden.BucketCapacity, s.Garden.BucketWater);
        Assert.False(Actions.Water(s, 0).Ok);
    }

    [Fact]
    public void ClearSkiesDoNothing()
    {
        var s = GameState.NewGame(1);
        s.Garden.Plots[0].Plant = new Plant(PepperSpecies.Bell);
        s.Garden.BucketWater = 1;
        WeatherInfo.ApplyRain(s);
        Assert.False(s.Garden.Plots[0].Plant!.WateredToday);
        Assert.Equal(1, s.Garden.BucketWater);
    }

    [Fact]
    public void SeedsPlantedInTheRainStartWet()
    {
        var s = GameState.NewGame(1);
        s.Weather = Weather.Rain;
        Assert.True(Actions.PlantSeed(s, 0, PepperSpecies.Bell).Ok);
        Assert.True(s.Garden.Plots[0].Plant!.WateredToday);

        var dry = GameState.NewGame(1);
        Assert.True(Actions.PlantSeed(dry, 0, PepperSpecies.Bell).Ok);
        Assert.False(dry.Garden.Plots[0].Plant!.WateredToday);
    }

    [Fact]
    public void SleepingIntoRainWatersEverythingAndReportsIt()
    {
        var s = GameState.NewGame(3);
        s.Garden.Plots[0].Plant = new Plant(PepperSpecies.Bell);
        bool sawRain = false;
        for (int day = 0; day < 80 && !sawRain; day++)
        {
            s.Garden.BucketWater = 0;
            var report = DayTick.Sleep(s);
            Assert.Equal(s.Weather, report.Weather);
            if (s.Weather != Weather.Rain) continue;
            sawRain = true;
            Assert.True(s.Garden.Plots[0].Plant!.WateredToday);
            Assert.Equal(Garden.BucketCapacity, s.Garden.BucketWater);
        }
        Assert.True(sawRain);
    }

    [Fact]
    public void WeatherSurvivesASave()
    {
        var s = GameState.NewGame(1);
        s.Weather = Weather.Windy;
        var back = SaveSystem.FromJson(SaveSystem.ToJson(s));
        Assert.Equal(Weather.Windy, back!.Weather);
    }
}

using SpiceWizard.Core;

namespace SpiceWizard.Core.Tests;

public class PlantTests
{
    static Plant Grown(PepperSpecies s, Action<Plant> care, int nights)
    {
        var p = new Plant(s);
        for (int i = 0; i < nights; i++)
        {
            care(p);
            p.EndOfNight();
        }
        return p;
    }

    [Fact]
    public void Bell_grows_one_point_per_watered_night_and_ignores_pep_talks()
    {
        var p = Grown(PepperSpecies.Bell, x => { x.WateredToday = true; x.PepTalkedToday = true; }, 3);
        Assert.True(p.IsMature);
        var dry = Grown(PepperSpecies.Bell, x => x.PepTalkedToday = true, 3);
        Assert.Equal(0, dry.Points);
    }

    [Fact]
    public void Banana_doubles_with_pep_talk_and_tolerates_alternate_watering()
    {
        var loved = Grown(PepperSpecies.Banana, x => { x.WateredToday = true; x.PepTalkedToday = true; }, 2);
        Assert.True(loved.IsMature);

        var p = new Plant(PepperSpecies.Banana);
        p.WateredToday = true; p.EndOfNight();   // watered today -> 1
        p.EndOfNight();                          // watered yesterday -> 1
        Assert.Equal(2, p.Points);
        p.EndOfNight();                          // dry two days -> 0
        Assert.Equal(2, p.Points);

        var pepOnly = Grown(PepperSpecies.Banana, x => x.PepTalkedToday = true, 2);
        Assert.Equal(0, pepOnly.Points);
    }

    [Fact]
    public void Bonnet_needs_a_pep_talk_to_grow_at_all()
    {
        var ignored = Grown(PepperSpecies.Bonnet, x => x.WateredToday = true, 4);
        Assert.Equal(0, ignored.Points);
        var adored = Grown(PepperSpecies.Bonnet, x => { x.WateredToday = true; x.PepTalkedToday = true; }, 3);
        Assert.True(adored.IsMature);
        var flattered = Grown(PepperSpecies.Bonnet, x => x.PepTalkedToday = true, 3);
        Assert.Equal(3, flattered.Points);
    }

    [Fact]
    public void Ghost_wants_alternate_watering_and_hides_when_pep_talked()
    {
        var p = new Plant(PepperSpecies.Ghost);
        p.WateredToday = true; p.EndOfNight();
        Assert.Equal(1, p.Points);
        p.EndOfNight();                          // yesterday only -> grows
        Assert.Equal(2, p.Points);
        p.WateredToday = true; p.EndOfNight();
        p.EndOfNight();
        Assert.True(p.IsMature);

        var drowned = Grown(PepperSpecies.Ghost, x => x.WateredToday = true, 3);
        Assert.Equal(1, drowned.Points);         // first night ok, then over-watered

        var spooked = new Plant(PepperSpecies.Ghost) { WateredToday = true, PepTalkedToday = true };
        Assert.Equal(0, spooked.GrowthTonight());
    }

    [Fact]
    public void Stages_follow_growth_points()
    {
        var p = new Plant(PepperSpecies.Bonnet);
        Assert.Equal(PlantStage.Seed, p.Stage);
        p.Points = 2; Assert.Equal(PlantStage.Sprout, p.Stage);
        p.Points = 3; Assert.Equal(PlantStage.Budding, p.Stage);
        p.Points = 6; Assert.Equal(PlantStage.Mature, p.Stage);
    }

    [Fact]
    public void Plots_and_jars_unlock_with_level()
    {
        Assert.Equal(4, Garden.UnlockedPlots(1));
        Assert.Equal(5, Garden.UnlockedPlots(3));
        Assert.Equal(8, Garden.UnlockedPlots(12));
        Assert.Equal(8, Garden.UnlockedPlots(20));
        Assert.Equal(2, FermentShelf.UnlockedJars(1));
        Assert.Equal(3, FermentShelf.UnlockedJars(5));
        Assert.Equal(3, FermentShelf.UnlockedJars(10));
        Assert.Equal(4, FermentShelf.UnlockedJars(11));
    }

    [Fact]
    public void Jars_ferment_in_two_nights_and_age_in_four()
    {
        var shelf = new FermentShelf();
        shelf.Jars[0].Species = PepperSpecies.Bell;
        shelf.EndOfNight();
        Assert.False(shelf.Jars[0].IsReady);
        shelf.EndOfNight();
        Assert.True(shelf.Jars[0].IsReady);
        Assert.False(shelf.Jars[0].IsAged);
        shelf.EndOfNight(); shelf.EndOfNight();
        Assert.True(shelf.Jars[0].IsAged);
        Assert.Equal(0, shelf.Jars[1].Nights);
    }
}

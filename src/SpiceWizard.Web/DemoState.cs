using SpiceWizard.Core;

namespace SpiceWizard.Web
{
    /// <summary>A mid-game state for screenshots and quick visual checks (open the page with ?demo).</summary>
    public static class DemoState
    {
        public static GameState Build(bool master)
        {
            var s = GameState.NewGame(99);
            s.Progression.Level = master ? 19 : 9;
            s.Progression.Xp = master ? Balance.XpToNext(19) - 1 : 40;
            s.Spice.SetMaxForLevel(s.Level);
            s.Spice.Refill();
            s.Peppercorns = 180;
            s.Clock.Day = 23;
            s.Clock.Minute = 13 * 60;

            s.Garden.Plots[0].Plant = new Plant(PepperSpecies.Bell) { Points = 3 };
            s.Garden.Plots[1].Plant = new Plant(PepperSpecies.Banana) { Points = 4 };
            s.Garden.Plots[2].Plant = new Plant(PepperSpecies.Bonnet) { Points = 6 };
            s.Garden.Plots[3].Plant = new Plant(PepperSpecies.Ghost) { Points = 4 };
            s.Garden.Plots[4].Plant = new Plant(PepperSpecies.Bell) { Points = 1, WateredToday = true };
            s.Garden.Plots[5].Plant = new Plant(PepperSpecies.Bonnet) { Points = 3, PepTalkedToday = true };
            s.Garden.Plots[6].Plant = new Plant(PepperSpecies.Ghost);

            s.Shelf.Jars[0].Species = PepperSpecies.Bell; s.Shelf.Jars[0].Nights = 4;
            s.Shelf.Jars[1].Species = PepperSpecies.Ghost; s.Shelf.Jars[1].Nights = 1;
            s.Shelf.Jars[2].Species = PepperSpecies.Bonnet; s.Shelf.Jars[2].Nights = 2;

            var inv = s.Inventory;
            inv.Peppers[0] = 5; inv.Peppers[1] = 3; inv.Peppers[2] = 2; inv.Peppers[3] = 1;
            inv.Powder[1] = 1; inv.Powder[3] = 1;
            inv.Mash[1] = 1; inv.AgedMash[0] = 1;
            inv.Seeds[0] = 2; inv.Seeds[2] = 1;
            for (int i = 0; i < Inventory.SpiceCount; i++) inv.Spices[i] = 2;
            inv.Sauces.Add(new Sauce(3, 4, 22));
            inv.Sauces.Add(new Sauce(7, 5, 23));
            var blend = new Blend { Peppercorns = 1 };
            blend.Powder[(int)PepperSpecies.Bonnet] = 1;
            blend.Spices[(int)Spice.Cumin] = 1; blend.Spices[(int)Spice.Coriander] = 1; blend.Spices[(int)Spice.Cloves] = 1;
            inv.Sauces.Add(new Sauce(blend, blend.Quality, 23));
            // A few blends the town has already tasted, so the board's Tasted page and the favourite have something to show.
            s.Town.Rate(new Sauce(new Blend { Powder = { [(int)PepperSpecies.Bell] = 1 }, Spices = { [(int)Spice.Cumin] = 1 } }, 2, 12), 12, null, s.Level);
            var rub = new Blend { Powder = { [(int)PepperSpecies.Bonnet] = 1 }, Peppercorns = 1 };
            s.Town.Rate(new Sauce(rub, rub.Quality, 16), 16, null, s.Level);
            s.Town.Rate(new Sauce(new Blend { Powder = { [(int)PepperSpecies.Banana] = 1 }, Spices = { [(int)Spice.Ginger] = 1, [(int)Spice.Cinnamon] = 1 } }, 3, 19), 19, null, s.Level);
            s.Crate.Sauces.Add(new Sauce(2, 3, 22));
            s.Crate.Sauces.Add(new Sauce(5, 4, 23));

            s.Quota = Quota.Generate(s.Clock.Week, s.Level, s.Rng, s.Town);
            s.Quota.Lines[0].Sold = s.Quota.Lines[0].Required;
            s.Rush = new RushOrder { RecipeId = 4, Count = 2, Delivered = 1, DueDay = s.Clock.Day + 1 };
            s.Stats.SaucesSold = 41; s.Stats.BlendsSold = 5; s.Stats.PeppercornsEarned = 2210; s.Stats.QuotasMet = 2; s.Stats.FiveStarSauces = 6;
            return s;
        }
    }
}

namespace SpiceWizard.Core;

/// <summary>
/// Every verb the player can perform, with all rule checks. The UI calls these and shows the message;
/// it never decides legality itself.
/// </summary>
public static class Actions
{
    // ---- Garden -------------------------------------------------------------

    public static ActionResult PlantSeed(GameState s, int plot, PepperSpecies species)
    {
        if (plot < 0 || plot >= s.UnlockedPlots) return ActionResult.Fail("That plot is still overgrown. Level up to clear it.");
        var p = s.Garden.Plots[plot];
        if (!p.IsEmpty) return ActionResult.Fail("Something is already growing there.");
        if (s.Inventory.Seed(species) <= 0) return ActionResult.Fail("No " + Species.NameOf(species) + " seeds.");
        s.Inventory.Seeds[(int)species]--;
        p.Plant = new Plant(species);
        WeatherInfo.ApplyRain(s);
        return ActionResult.Success("Planted a " + Species.NameOf(species) + " seed." + (s.Weather == Weather.Rain ? " The rain soaks it in." : ""));
    }

    public static ActionResult Water(GameState s, int plot)
    {
        var plant = PlantAt(s, plot);
        if (plant == null) return ActionResult.Fail("Nothing to water.");
        if (plant.IsMature) return ActionResult.Fail("It is done growing. Harvest it!");
        if (plant.WateredToday) return ActionResult.Fail(s.Weather == Weather.Rain ? "The rain has that covered." : "Already watered today.");
        if (s.Garden.BucketWater <= 0) return ActionResult.Fail("The bucket is empty. Refill at the well.");
        s.Garden.BucketWater--;
        plant.WateredToday = true;
        return ActionResult.Success("Watered. " + plant.Mood());
    }

    public static ActionResult RefillBucket(GameState s)
    {
        if (s.Garden.BucketWater >= Garden.BucketCapacity) return ActionResult.Fail("The bucket is already full.");
        s.Garden.BucketWater = Garden.BucketCapacity;
        return ActionResult.Success("Bucket filled.");
    }

    public static ActionResult PepTalk(GameState s, int plot)
    {
        var plant = PlantAt(s, plot);
        if (plant == null) return ActionResult.Fail("Nobody to encourage.");
        if (plant.IsMature) return ActionResult.Fail("It is done growing. Harvest it!");
        if (plant.PepTalkedToday) return ActionResult.Fail("One pep talk a day is plenty.");
        if (!s.Spice.TrySpend(Balance.PepTalkSpiceCost)) return ActionResult.Fail("Not enough spice in you for a speech.");
        plant.PepTalkedToday = true;
        return ActionResult.Success("\"You can do it!\" " + plant.Mood());
    }

    public static ActionResult Harvest(GameState s, int plot)
    {
        var plant = PlantAt(s, plot);
        if (plant == null) return ActionResult.Fail("Nothing to harvest.");
        if (!plant.IsMature) return ActionResult.Fail("Not ripe yet. " + plant.Mood());
        var info = plant.Info;
        s.Inventory.Peppers[(int)plant.Species] += info.Yield;
        s.Garden.Plots[plot].Plant = null;
        return ActionResult.Success($"Harvested {info.Yield} {info.Name} pepper{(info.Yield == 1 ? "" : "s")}.");
    }

    // ---- Hasten spell -------------------------------------------------------

    /// <summary>Why the hasten spell cannot be cast right now, or empty when it can.</summary>
    public static string HastenBlocker(GameState s)
    {
        if (s.HastenedToday) return "The hasten spell is spent for today.";
        if (!s.Spice.CanSpend(Balance.HastenSpiceCost)) return $"Hastening takes {Balance.HastenSpiceCost} spice. Eat a pepper or rest.";
        return "";
    }

    public static ActionResult HastenPlant(GameState s, int plot)
    {
        var plant = PlantAt(s, plot);
        if (plant == null) return ActionResult.Fail("Nothing to hasten.");
        if (plant.IsMature) return ActionResult.Fail("It is done growing. Harvest it!");
        string blocker = HastenBlocker(s);
        if (blocker.Length > 0) return ActionResult.Fail(blocker);
        s.Spice.TrySpend(Balance.HastenSpiceCost);
        s.HastenedToday = true;
        plant.Hasten(Balance.HastenNights);
        return ActionResult.Success("Time hurries along! " + plant.Mood());
    }

    public static ActionResult HastenJar(GameState s, int jar)
    {
        if (jar < 0 || jar >= s.UnlockedJars) return ActionResult.Fail("You do not own that jar yet.");
        var j = s.Shelf.Jars[jar];
        if (j.IsEmpty) return ActionResult.Fail("The jar is empty.");
        if (j.IsAged) return ActionResult.Fail("It cannot age any further.");
        string blocker = HastenBlocker(s);
        if (blocker.Length > 0) return ActionResult.Fail(blocker);
        s.Spice.TrySpend(Balance.HastenSpiceCost);
        s.HastenedToday = true;
        j.Nights = Math.Min(Jar.NightsToAge, j.Nights + Balance.HastenNights);
        return ActionResult.Success("Time hurries along! " + j.Status());
    }

    static Plant? PlantAt(GameState s, int plot) =>
        plot < 0 || plot >= Garden.MaxPlots ? null : s.Garden.Plots[plot].Plant;

    // ---- Eating -------------------------------------------------------------

    public static ActionResult Eat(GameState s, PepperSpecies species)
    {
        if (s.Inventory.Pepper(species) <= 0) return ActionResult.Fail("No " + Species.NameOf(species) + " peppers to eat.");
        if (s.Spice.Current >= s.Spice.Max) return ActionResult.Fail("You are already full of spice.");
        s.Inventory.Peppers[(int)species]--;
        int heat = Species.Get(species).Heat;
        s.Spice.Restore(heat);
        return ActionResult.Success($"Crunch! +{heat} spice.");
    }

    // ---- Fermenting ---------------------------------------------------------

    public static ActionResult FillJar(GameState s, int jar, PepperSpecies species)
    {
        if (jar < 0 || jar >= s.UnlockedJars) return ActionResult.Fail("You do not own that jar yet.");
        var j = s.Shelf.Jars[jar];
        if (!j.IsEmpty) return ActionResult.Fail("That jar is in use.");
        if (s.Inventory.Pepper(species) < Jar.PeppersPerJar) return ActionResult.Fail($"Need {Jar.PeppersPerJar} {Species.NameOf(species)} peppers.");
        s.Inventory.Peppers[(int)species] -= Jar.PeppersPerJar;
        j.Species = species;
        j.Nights = 0;
        return ActionResult.Success($"{Species.NameOf(species)} peppers packed. Ready in {Jar.NightsToFerment} nights.");
    }

    public static ActionResult EmptyJar(GameState s, int jar)
    {
        if (jar < 0 || jar >= FermentShelf.MaxJars) return ActionResult.Fail("No such jar.");
        var j = s.Shelf.Jars[jar];
        if (j.IsEmpty) return ActionResult.Fail("The jar is empty.");
        if (!j.IsReady) return ActionResult.Fail("Still fermenting. " + j.Status());
        var species = j.Species!.Value;
        bool aged = j.IsAged;
        if (aged) s.Inventory.AgedMash[(int)species]++;
        else s.Inventory.Mash[(int)species]++;
        j.Species = null;
        j.Nights = 0;
        return ActionResult.Success((aged ? "Aged " : "") + Species.NameOf(species) + " mash collected.");
    }

    // ---- Mortar -------------------------------------------------------------

    public static ActionResult Grind(GameState s, PepperSpecies species)
    {
        if (s.Inventory.Pepper(species) <= 0) return ActionResult.Fail("No " + Species.NameOf(species) + " peppers to grind.");
        if (!s.Spice.CanSpend(Balance.GrindSpiceCost)) return ActionResult.Fail("Too tired to grind. Eat a pepper or rest.");
        s.Spice.TrySpend(Balance.GrindSpiceCost);
        s.Inventory.Peppers[(int)species]--;
        s.Inventory.Powder[(int)species]++;
        return ActionResult.Success("Ground into " + Species.NameOf(species) + " powder.");
    }

    // ---- Cooking ------------------------------------------------------------

    public static int Have(GameState s, Ingredient ing) => ing.Kind switch
    {
        IngredientKind.Pepper => s.Inventory.Peppers[ing.Index],
        IngredientKind.Powder => s.Inventory.Powder[ing.Index],
        IngredientKind.Mash => s.Inventory.MashOf((PepperSpecies)ing.Index),
        IngredientKind.Spice => s.Inventory.Spices[ing.Index],
        _ => s.Peppercorns,
    };

    /// <summary>Why the recipe cannot be cooked right now, or empty when it can.</summary>
    public static string CookBlocker(GameState s, Recipe recipe, bool extraPeppercorn)
    {
        if (recipe.UnlockLevel > s.Level) return $"Unlocks at level {recipe.UnlockLevel}.";
        if (!s.Spice.CanSpend(Balance.CookSpiceCost)) return $"Cooking takes {Balance.CookSpiceCost} spice. Eat a pepper or rest.";
        int peppercornsNeeded = extraPeppercorn ? 1 : 0;
        foreach (var ing in recipe.Ingredients)
        {
            if (ing.Kind == IngredientKind.Peppercorn) { peppercornsNeeded += ing.Count; continue; }
            if (Have(s, ing) < ing.Count) return $"Missing {ing.Name}.";
        }
        if (s.Peppercorns < peppercornsNeeded) return "Not enough peppercorns.";
        return "";
    }

    public static ActionResult Cook(GameState s, int recipeId, bool extraPeppercorn)
    {
        var recipe = RecipeBook.Get(recipeId);
        string blocker = CookBlocker(s, recipe, extraPeppercorn);
        if (blocker.Length > 0) return ActionResult.Fail(blocker);

        bool usedAged = false;
        foreach (var ing in recipe.Ingredients)
            if (Consume(s, ing)) usedAged = true;
        if (extraPeppercorn) s.Peppercorns--;
        s.Spice.TrySpend(Balance.CookSpiceCost);

        int quality = Quality(recipe, s.Level, usedAged, extraPeppercorn);
        s.Inventory.Sauces.Add(new Sauce(recipe.Id, quality, s.Clock.Day));
        return ActionResult.Success($"{recipe.Name} bottled. {new string('*', quality)}");
    }

    /// <summary>Takes one ingredient line out of the pantry. Returns true when aged mash went in.</summary>
    static bool Consume(GameState s, Ingredient ing)
    {
        switch (ing.Kind)
        {
            case IngredientKind.Pepper: s.Inventory.Peppers[ing.Index] -= ing.Count; break;
            case IngredientKind.Powder: s.Inventory.Powder[ing.Index] -= ing.Count; break;
            case IngredientKind.Spice: s.Inventory.Spices[ing.Index] -= ing.Count; break;
            case IngredientKind.Peppercorn: s.Peppercorns -= ing.Count; break;
            case IngredientKind.Mash:
                // Aged mash is strictly better, so it is always used first.
                int fromAged = Math.Min(ing.Count, s.Inventory.AgedMash[ing.Index]);
                s.Inventory.AgedMash[ing.Index] -= fromAged;
                s.Inventory.Mash[ing.Index] -= ing.Count - fromAged;
                return fromAged > 0;
        }
        return false;
    }

    public static int Quality(Recipe recipe, int level, bool usedAged, bool extraPeppercorn)
    {
        int q = 3;
        if (usedAged) q++;
        if (extraPeppercorn) q++;
        if (level < recipe.UnlockLevel + 2) q--;
        if (level >= recipe.UnlockLevel + 6) q++;
        return Math.Clamp(q, 1, 5);
    }

    // ---- Blending -----------------------------------------------------------

    /// <summary>Why the blend cannot be mixed right now, or empty when it can.</summary>
    public static string BlendBlocker(GameState s, Blend blend)
    {
        if (s.Level < Balance.BlendUnlockLevel) return $"Blending unlocks at level {Balance.BlendUnlockLevel}.";
        if (blend.Pinches < Balance.MinBlendPinches) return $"A blend needs at least {Balance.MinBlendPinches} pinches.";
        if (blend.Pinches > Balance.MaxBlendPinches) return $"At most {Balance.MaxBlendPinches} pinches fit in the mortar.";
        if (!s.Spice.CanSpend(Balance.BlendSpiceCost)) return $"Blending takes {Balance.BlendSpiceCost} spice. Eat a pepper or rest.";
        foreach (var ing in blend.Ingredients)
        {
            if (Have(s, ing) >= ing.Count) continue;
            return ing.Kind == IngredientKind.Peppercorn ? "Not enough peppercorns." : $"Missing {ing.Name}.";
        }
        return "";
    }

    /// <summary>Mixes a copy of <paramref name="blend"/> and bottles it like a sauce; the draft is left as it was.</summary>
    public static ActionResult MakeBlend(GameState s, Blend blend)
    {
        string blocker = BlendBlocker(s, blend);
        if (blocker.Length > 0) return ActionResult.Fail(blocker);

        var made = blend.Clone();
        foreach (var ing in made.Ingredients) Consume(s, ing);
        s.Spice.TrySpend(Balance.BlendSpiceCost);
        s.Inventory.Sauces.Add(new Sauce(made, made.Quality, s.Clock.Day));
        return ActionResult.Success($"{made.Name} mixed. {new string('*', made.Quality)}");
    }

    // ---- Market -------------------------------------------------------------

    public static ActionResult BuySeed(GameState s, PepperSpecies species)
    {
        var info = Species.Get(species);
        if (info.UnlockLevel > s.Level) return ActionResult.Fail($"{info.Name} seeds unlock at level {info.UnlockLevel}.");
        if (s.Peppercorns < info.SeedCost) return ActionResult.Fail("Not enough peppercorns.");
        s.Peppercorns -= info.SeedCost;
        s.Inventory.Seeds[(int)species]++;
        return ActionResult.Success($"Bought {info.Name} seed for {info.SeedCost} peppercorns.");
    }

    public static ActionResult BuySpice(GameState s, Spice spice)
    {
        int unlock = SpiceInfo.UnlockLevel(spice);
        if (unlock > s.Level) return ActionResult.Fail($"{SpiceInfo.Name(spice)} unlocks at level {unlock}.");
        int price = SpiceInfo.Price(spice);
        if (s.Peppercorns < price) return ActionResult.Fail("Not enough peppercorns.");
        s.Peppercorns -= price;
        s.Inventory.Spices[(int)spice]++;
        return ActionResult.Success($"Bought {SpiceInfo.Name(spice)} for {price} peppercorns.");
    }

    // ---- Shipping -----------------------------------------------------------

    public static ActionResult Ship(GameState s, int sauceIndex)
    {
        if (sauceIndex < 0 || sauceIndex >= s.Inventory.Sauces.Count) return ActionResult.Fail("No such sauce.");
        if (s.Crate.IsFull) return ActionResult.Fail("The crate is full.");
        var sauce = s.Inventory.Sauces[sauceIndex];
        s.Inventory.Sauces.RemoveAt(sauceIndex);
        s.Crate.Sauces.Add(sauce);
        return ActionResult.Success(sauce.Name + " packed for town.");
    }

    public static ActionResult Unship(GameState s, int crateIndex)
    {
        if (crateIndex < 0 || crateIndex >= s.Crate.Sauces.Count) return ActionResult.Fail("Nothing there.");
        var sauce = s.Crate.Sauces[crateIndex];
        s.Crate.Sauces.RemoveAt(crateIndex);
        s.Inventory.Sauces.Add(sauce);
        return ActionResult.Success(sauce.Name + " taken back.");
    }

    // ---- Night --------------------------------------------------------------

    public static ActionResult Sleep(GameState s)
    {
        var report = DayTick.Sleep(s);
        return ActionResult.Success($"Day {report.Day} dawns.");
    }
}

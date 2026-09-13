using SpiceWizard.Core;

namespace SpiceWizard.Core.Tests;

/// <summary>
/// A headless, reasonably competent player. It is used to check that the numbers in
/// <see cref="Balance"/> give a game of the intended length and that nothing soft-locks.
/// </summary>
public sealed class GreedyBot
{
    public GameState State { get; }
    public int MaxDays { get; }
    public List<string> Log { get; } = new();

    public GreedyBot(ulong seed, int maxDays = 80)
    {
        State = GameState.NewGame(seed);
        MaxDays = maxDays;
    }

    /// <summary>Plays until level 20 or the day cap. Returns the day mastery was reached, or -1.</summary>
    public int Play()
    {
        while (!State.Won && State.Clock.Day <= MaxDays)
        {
            PlayDay();
            Assert.True(State.Peppercorns >= 0, "peppercorns went negative");
            DayTick.Sleep(State);
        }
        return State.Won ? State.WonOnDay : -1;
    }

    void PlayDay()
    {
        var s = State;
        for (int i = 0; i < Garden.MaxPlots; i++) Actions.Harvest(s, i);
        for (int i = 0; i < FermentShelf.MaxJars; i++) Actions.EmptyJar(s, i);

        CookEverything();
        ShipEverything();
        MixBlends();
        ShipEverything();

        FillJars();
        PlantSeeds();
        TendPlants();
    }

    void ShipEverything()
    {
        var s = State;
        for (int i = s.Inventory.Sauces.Count - 1; i >= 0 && !s.Crate.IsFull(s.Level); i--) Actions.Ship(s, i);
    }

    /// <summary>
    /// Spends whatever spice is left after cooking on blends: one pinch of the hottest spare pepper, a
    /// peppercorn and up to three cheap spices. Varies the spices so the town keeps finding them new.
    /// </summary>
    void MixBlends()
    {
        var s = State;
        if (s.Level < Balance.BlendUnlockLevel) return;
        for (int made = 0; made < 2; made++)
        {
            PepperSpecies? pick = null;
            foreach (var sp in new[] { PepperSpecies.Ghost, PepperSpecies.Bonnet, PepperSpecies.Banana, PepperSpecies.Bell })
                if (s.Inventory.PowderOf(sp) > 0 || s.Inventory.Pepper(sp) > (sp == PepperSpecies.Bell ? 4 : 2)) { pick = sp; break; }
            if (pick == null) return;
            int needed = Balance.BlendSpiceCost + (s.Inventory.PowderOf(pick.Value) == 0 ? Balance.GrindSpiceCost : 0);
            if (!s.Spice.CanSpend(needed)) return;
            if (s.Inventory.PowderOf(pick.Value) == 0 && !Actions.Grind(s, pick.Value).Ok) return;

            var blend = new Blend();
            blend.Powder[(int)pick.Value] = 1;
            if (s.Peppercorns > 10) blend.Peppercorns = 1;
            var spices = Enum.GetValues<Spice>().Where(sp => SpiceInfo.UnlockLevel(sp) <= s.Level).OrderBy(SpiceInfo.Price).ToList();
            int start = s.Town.TastedBlends.Count % spices.Count;
            for (int k = 0; k < spices.Count && blend.Pinches < Balance.MaxBlendPinches && blend.DistinctSpices < Balance.BlendAromaticSpices; k++)
            {
                var spice = spices[(start + k) % spices.Count];
                if (s.Inventory.SpiceOf(spice) == 0)
                {
                    if (s.Peppercorns - SpiceInfo.Price(spice) < 20) continue;
                    if (!Actions.BuySpice(s, spice).Ok) continue;
                }
                blend.Spices[(int)spice] = 1;
            }
            if (!Actions.MakeBlend(s, blend).Ok) return;
        }
    }

    void CookEverything()
    {
        var s = State;
        var recipes = RecipeBook.UnlockedAt(s.Level).OrderByDescending(r => r.BaseValue).ToList();
        bool progress = true;
        while (progress && s.Spice.CanSpend(Balance.CookSpiceCost))
        {
            progress = false;
            foreach (var r in recipes)
            {
                // Avoid boring the town: at most two of the same sauce in the crate.
                if (s.Inventory.Sauces.Count(x => x.RecipeId == r.Id) + s.Crate.Sauces.Count(x => x.RecipeId == r.Id) >= 2) continue;
                if (!TryGather(r)) continue;
                bool extra = s.Peppercorns > 25;
                var res = Actions.Cook(s, r.Id, extra);
                if (res.Ok) { progress = true; break; }
            }
        }
        // Spend spare spice on eating only when a cook is one pepper away.
        if (!s.Spice.CanSpend(Balance.CookSpiceCost) && s.Inventory.Pepper(PepperSpecies.Bell) > 4)
            Actions.Eat(s, PepperSpecies.Bell);
    }

    /// <summary>Buys spices and grinds powder so that <paramref name="r"/> can be cooked. False if mash or peppers are missing.</summary>
    bool TryGather(Recipe r)
    {
        var s = State;
        int peppercornsNeeded = 0;
        foreach (var ing in r.Ingredients)
        {
            int have = Actions.Have(s, ing);
            switch (ing.Kind)
            {
                case IngredientKind.Peppercorn: peppercornsNeeded += ing.Count; break;
                case IngredientKind.Mash:
                case IngredientKind.Pepper:
                    if (have < ing.Count) return false;
                    break;
                case IngredientKind.Powder:
                    while (have < ing.Count)
                    {
                        if (!Actions.Grind(s, (PepperSpecies)ing.Index).Ok) return false;
                        have++;
                    }
                    break;
                case IngredientKind.Spice:
                    while (have < ing.Count)
                    {
                        if (s.Peppercorns - SpiceInfo.Price((Spice)ing.Index) < peppercornsNeeded + 5) return false;
                        if (!Actions.BuySpice(s, (Spice)ing.Index).Ok) return false;
                        have++;
                    }
                    break;
            }
        }
        return s.Peppercorns >= peppercornsNeeded;
    }

    void FillJars()
    {
        var s = State;
        for (int j = 0; j < s.UnlockedJars; j++)
        {
            if (!s.Shelf.Jars[j].IsEmpty) continue;
            // Prefer the hottest species we have a hot-sauce recipe for and enough peppers of.
            foreach (var sp in new[] { PepperSpecies.Ghost, PepperSpecies.Bonnet, PepperSpecies.Banana, PepperSpecies.Bell })
            {
                bool hasRecipe = RecipeBook.UnlockedAt(s.Level).Any(r => r.Ingredients.Any(i => i.Kind == IngredientKind.Mash && i.Index == (int)sp));
                if (!hasRecipe) continue;
                // Keep two peppers back for curries that use fresh peppers or powder.
                int spare = s.Inventory.Pepper(sp) - (sp == PepperSpecies.Bell ? 2 : 1);
                if (spare >= Jar.PeppersPerJar && Actions.FillJar(s, j, sp).Ok) break;
            }
        }
    }

    void PlantSeeds()
    {
        var s = State;
        for (int p = 0; p < s.UnlockedPlots; p++)
        {
            if (!s.Garden.Plots[p].IsEmpty) continue;
            // Best unlocked species we can afford while keeping a small float for spices. Odd plots take the
            // runner-up so the late recipes that mix two peppers (Rainbow Chutney, Wizard's Curry) stay cookable.
            var choices = Species.All.Where(i => i.UnlockLevel <= s.Level).OrderByDescending(i => i.UnlockLevel).ToList();
            if (p % 2 == 1 && choices.Count > 1) choices.RemoveAt(0);
            foreach (var info in choices)
            {
                if (s.Inventory.Seed(info.Species) == 0)
                {
                    if (s.Peppercorns - info.SeedCost < 8) continue;
                    if (!Actions.BuySeed(s, info.Species).Ok) continue;
                }
                if (Actions.PlantSeed(s, p, info.Species).Ok) break;
            }
        }
    }

    void TendPlants()
    {
        var s = State;
        for (int p = 0; p < s.UnlockedPlots; p++)
        {
            var plant = s.Garden.Plots[p].Plant;
            if (plant == null || plant.IsMature) continue;
            bool water = plant.Species switch
            {
                PepperSpecies.Ghost => !plant.WateredYesterday,
                PepperSpecies.Banana => !plant.WateredYesterday,
                _ => true,
            };
            bool pep = plant.Species switch
            {
                PepperSpecies.Bonnet => true,
                PepperSpecies.Banana => s.Spice.Current > Balance.PepTalkSpiceCost,
                _ => false,
            };
            if (water)
            {
                if (s.Garden.BucketWater == 0) Actions.RefillBucket(s);
                Actions.Water(s, p);
            }
            if (pep && !Actions.PepTalk(s, p).Ok && plant.Species == PepperSpecies.Bonnet)
            {
                // A bonnet without its pep talk stalls; eat something to afford the speech.
                foreach (var sp in new[] { PepperSpecies.Bell, PepperSpecies.Banana })
                    if (Actions.Eat(s, sp).Ok) { Actions.PepTalk(s, p); break; }
            }
        }
    }
}

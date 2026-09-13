# Spice Wizard

A one-screen, pixel-art cooking and farming simulator built for a weekend game jam around the themes **Curry** and **Pepper**. Runs in the browser on the [KNI engine](https://github.com/kniEngine/kni) (Blazor WebAssembly).

You are the Spice Wizard. Grow peppers in the garden outside your tower, ferment and grind them, brew hot sauces and curries in the cauldron, mix spice blends of your own at the mortar, and ship them to the nearby town. The town rates every bottle, pays you in peppercorns and posts a weekly request on the notice board. Climb from level 1 to level 20 and the whole town turns up to celebrate the new **Master Spice Wizard**.

## Play

Requires the .NET 8 SDK (or newer).

```bash
dotnet run --project src/SpiceWizard.Web
```

Open the printed URL (`http://localhost:5259` by default). The whole game is one screen: click anything in the yard to use it.

```bash
dotnet test                                    # simulation tests + balance bot
dotnet publish src/SpiceWizard.Web -c Release  # static site in bin/Release/net8.0/publish/wwwroot
```

Add `?demo` to the URL for a mid-game state, `?demo=master` to be one night away from winning, `?demo=night` for the same mid-game state at dusk, `?demo=windy` / `?demo=rain` for the weather, or `?demo=chores` for a day-one garden that is fully planted and watered (the early-bedtime nudge shows at once).

The yard is drawn at 384x216 pixels and scaled up by the largest whole number that fits your window; whatever room is left is filled with more sky and meadow, so there are no black bars. Resize the window and the game follows.

## Controls

| Action | Input |
| --- | --- |
| Use a station | Click it — the wizard walks over and a panel opens |
| Close a panel | The `x` button or `Esc` |
| Pause | `Esc` with nothing open: **Resume**, **Options** or **Quit to title** |
| Help | The `?` button in the top-right corner |
| Go to bed early | The moon button in the top-right corner, or the tower door |

Mouse (or touch) only. The game autosaves every morning to your browser's local storage; **Continue** on the title screen picks up where you left off, and **New game** asks before it overwrites a save. Quitting to the title from the pause menu also saves.

## Options and sound

The pause menu's **Options** screen has three volume sliders — **Music**, **Ambience** and **SFX** — which are remembered in local storage separately from the save.

- **Music**: three tunes, one per day in rotation (*Morning Meadow*, a G major waltz; *Simmering Pot*, a bouncing D dorian tune with bubbling blips; *Turmeric Sun*, slow F lydian pads). Music plays in daylight only, fading out at dusk and back in at dawn.
- **Ambience**: the wind always blows in slow gusts; birds chirp by day and crickets take over at night.
- **SFX**: clicks, watering splashes, sparkles and the morning chime.

The gusts you hear are the same ones that sway the trees, bushes, flowers and garden plants — the meadow leans with the wind. Everything is synthesised in code at start-up; there are no audio files.

## The day

A day runs from 06:00 to 22:00 in six real minutes. The clock pauses while a panel is open, so reading and shopping are free. At 22:00 (or when you click the tower door or the moon button) you sleep, and the night moves everything on at once. Once every plant is watered and nothing is waiting to be harvested or shipped, the wizard suggests turning in early.

1. Jars ferment one night and plants grow.
2. The cart takes whatever is in the shipping crate to town; each sauce is rated and paid for.
3. On the first morning of a week the previous quota is judged and a new one is posted.
4. Your spice refills, and the morning report shows what happened.

## The yard

| Station | What it does |
| --- | --- |
| **Garden plots** | Plant a seed, water it, give it a pep talk, harvest it. 4 plots to start, 8 by level 12. |
| **Well** | Refills the watering bucket (4 waters). |
| **Merchant cart** | Sells seeds and spices for peppercorns. Higher-level goods unlock as you level. |
| **Notice board** | This week's quota and its progress. |
| **Cauldron** | Cooks the ten recipes. Costs 3 spice per sauce. |
| **Jar shelf** | Ferments 2 peppers of one kind into mash in 2 nights; leave it 4 nights for aged mash (3 from level 18). 2 jars to start, 4 by level 11. |
| **Mortar** | Grinds 1 pepper into 1 powder for 1 spice. From level 2 it also mixes powder, spices and peppercorns into your own **spice blends** for 2 spice. |
| **Pantry** | Everything you own. Eat peppers here to restore spice. |
| **Shipping crate** | Holds up to 6 sauces or blends for tonight's delivery (8 from level 14). |
| **Tower door** | Go to bed early. |

Three cats (a ginger tabby, a grey and a smoky black) also live in the yard. They stroll about, nap in the grass, roll over, scratch the trees and the stump, hide in the bushes with only their eyes showing, and jump up onto the fence, the crate, the pantry, the well and the branches. They are purely decorative and never get in the way of anything.

## Resources

- **Starter kit**: 30 peppercorns, 4 Bell seeds, 1 Banana seed, 1 jar of Bell mash and one pinch each of Cumin, Coriander and Curry Leaves, so a Bell Hot Sauce can go to town on the very first night and a Golden Curry after the first harvest.
- **Peppercorns** are the town's currency *and* an ingredient. Recipes that call for peppercorns spend them from your purse.
- **Spice** is your cooking energy: 8 at level 1, +1 every three levels (max 14). Cooking costs 3, blending 2, grinding 1, a pep talk 1, the hasten spell 2 (pushes a plant or jar 2 nights ahead; once a day, twice from level 16). Sleep refills it; eating a pepper restores its heat value.
- **Spices** from the merchant: Cumin 3, Cinnamon 3, Curry Leaves 4, Coriander 3, Ginger 4 (level 1); Cardamom 6 (level 3); Cloves 6 and Fenugreek 5 (level 5). Meeting a quota also grants one rare spice.

## Peppers

Each pepper looks like its name and has its own temperament. Growth is counted in points earned overnight.

| Pepper | Looks like | Growth needed | Grows when | Yield | Heat | Seed | Unlocks |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Bell** | a bronze bell | 3 | watered today (+1). Ignores pep talks. | 3 | +1 | 5 pc | level 1 |
| **Banana** | a banana | 4 | watered today or yesterday (+1), +1 more if pep-talked | 2 | +2 | 8 pc | level 1 |
| **Bonnet** | a frilly bonnet hat | 6 | pep-talked (+1), +1 more if watered. No pep talk, no growth. | 2 | +3 | 12 pc | level 4 |
| **Ghost** | the ghost of a chili | 4 | watered exactly once in the last two days (+1). Over-watering stalls it; a pep talk scares it into hiding. | 1 | +5 | 20 pc | level 8 |

Watering and pep talks are once per plant per day. A harvested plot is empty again.

## Recipes

Hot sauces need fermented mash; curries need fresh peppers or powder.

| Sauce | Type | Ingredients | Value | Unlocks |
| --- | --- | --- | --- | --- |
| Bell Hot Sauce | hot | Bell mash, 1 peppercorn | 15 | level 1 |
| Golden Curry | curry | 2 Bell peppers, Cumin, Coriander, Curry Leaves | 16 | level 1 |
| Banana Blaze | hot | Banana mash, Ginger, 1 peppercorn | 25 | level 2 |
| Sunset Curry | curry | Banana powder, Cinnamon, Cardamom, Ginger | 24 | level 4 |
| Bonnet Fire | hot | Bonnet mash, Coriander, Cumin, 2 peppercorns | 40 | level 5 |
| Bonnet Curry | curry | Bonnet powder, Fenugreek, Cumin, Curry Leaves, Cloves | 40 | level 7 |
| Phantom Sauce | hot | Ghost mash, Cloves, Cinnamon, 2 peppercorns | 70 | level 10 |
| Spectral Curry | curry | Ghost powder, Cardamom, Cloves, Fenugreek, 3 peppercorns | 70 | level 13 |
| Rainbow Chutney | hot | Bonnet mash, Banana mash, Ginger, Cinnamon, 2 peppercorns | 85 | level 15 |
| Wizard's Curry | curry | Ghost powder, Bonnet powder, Cardamom, Fenugreek, Cloves, 3 peppercorns | 100 | level 17 |

Hot sauces pay a quarter more than the curry of their tier to make up for the jar and the two nights of fermenting.

Every sauce is bottled with a **quality** of 1–5 stars: 3 by default, +1 for aged mash, +1 for adding an extra peppercorn, −1 while the recipe is new to you (below its unlock level + 2), +1 once mastered (unlock level + 6).

## Spice blends

From level 2 the mortar also mixes blends of your own design: **2 to 5 pinches** of pepper powder, merchant spices and peppercorns, for 2 spice. No recipe, no fermenting — a blend is ready the moment it is mixed and sells like a sauce.

- **Worth**: the pinches' merchant prices plus 20%. A pinch of powder is worth 2 + 2 × the pepper's heat (Bell 4, Banana 6, Bonnet 8, Ghost 12); a peppercorn pinch is worth 2. The tier (for fame) follows the recipe book's value bands.
- **Quality**: a plain mix is 2 stars. **+1** when the powders' total heat is 3 or more, **+1** with three different spices, **+1** with a peppercorn in it, **−1** with no pepper powder at all.
- **Name**: the hottest pepper in it plus what it is — *Bonnet Masala* (three spices), *Ghost Rub* (peppercorn), *Banana Blend*, *Bell Dust*.
- Blends with exactly the same pinches are the same blend to the town. Every **new** blend earns an extra star the first time the town tastes it; repeats bore the town like any sauce. Blends never count toward the weekly quota.

## Selling

Overnight the town rates each bottle in the crate: quality, **+1 star** if the sauce is on this week's notice, **+1 star** if it is the type the town craves this week, **+1 star** for a blend the town has never tasted, **−1 star** if the town has already had two of that sauce or blend in the last three days (three from level 19). Pay is the product's value × 0.5 / 0.8 / 1 / 1.4 / 2 for 1–5 stars; fame (XP) is stars × tier × 6.

## Weekly quota

Every week the notice board asks for two or three sauces you can already cook. Deliver them all by the end of day 7 for a bonus of 40 + 25 × week peppercorns, 30 + 10 × week fame and a rare spice.

Half the weeks the town also **craves** one kind of sauce — hot sauces on odd weeks, curries on even ones — and every bottle of that kind earns an extra star all week. The board and the morning report say when it does.

## Progression

Fame to the next level is 25 + 18 × (level − 1). Every level from 2 to 20 unlocks something; hover the level bar for the next one.

| Level | Unlock |
| --- | --- |
| 2 | spice blends |
| 3 | plot 5, Cardamom |
| 4 | Bonnet seeds, Sunset Curry, +1 spice |
| 5 | Bonnet Fire, jar 3, Cloves, Fenugreek |
| 6 | plot 6 |
| 7 | Bonnet Curry, +1 spice |
| 8 | Ghost seeds |
| 9 | plot 7 |
| 10 | Phantom Sauce, +1 spice |
| 11 | jar 4 |
| 12 | plot 8 |
| 13 | Spectral Curry, +1 spice |
| 14 | the crate holds 8 |
| 15 | Rainbow Chutney |
| 16 | a second hasten spell each day, +1 spice |
| 17 | Wizard's Curry |
| 18 | mash ages in 3 nights |
| 19 | the town forgives one more repeat, +1 spice |
| 20 | Master Spice Wizard — the town celebrates |

Level 20 wins the game; you can keep playing afterwards. A competent player gets there in roughly a month of game days — the balance bot in the tests does it in a median of 29.

## Project structure

```
SpiceWizard.sln
src/SpiceWizard.Core/          pure simulation, no engine references
  Species.cs, Plant.cs, Garden.cs (plots + jars), Recipes.cs (+ Balance), Blends.cs,
  Town.cs (ratings, novelty, quota), Progression.cs (+ SpiceMeter, GameClock),
  Actions.cs (every player verb), DayTick.cs (night resolution), SaveSystem.cs
src/SpiceWizard.Web/           KNI Blazor WebAssembly host
  Art/      palette, 5x7 pixel font, sprites as text, runtime atlas, canvas helpers
  Scene/    layout of the single screen, renderer, day/night, wizard, particles, crowd, cats
  Ui/       immediate-mode widgets, station panels, HUD/title/pause/options overlays, settings
  Audio/    software synth, the three music tracks, wind/birds/crickets, effects, mixer
  SpiceWizardGame.cs, DemoState.cs, Pages/Index.razor(.cs), wwwroot/index.html
tests/SpiceWizard.Core.Tests/  xunit: one file per system + GreedyBot balance playthrough
```

All art is generated at start-up from text sprites and a shared palette, and all sound is rendered from note lists and noise by a tiny synth, so there is no content pipeline; the KNI packages are pulled from NuGet.

Balance numbers live in `Balance` (`src/SpiceWizard.Core/Recipes.cs`), `Species.All` and `RecipeBook.All`. Change them, then run `dotnet test` — `BotPlaythroughTests` asserts the game is still winnable in a sensible number of days.

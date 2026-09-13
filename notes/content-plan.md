# Spice Wizard — phased content plan

Implements [content-review.md](content-review.md). Each phase is shippable on
its own and ordered by value-per-hour for a jam build; stop after any phase
and the game is better than before it. Every phase ends with `dotnet build`,
`dotnet test` (bot median must stay 20–40 days) and a README update, per
`instructions/general-instructions.md`.

Conventions that hold throughout:
- Rules and numbers live in Core (`Balance`, `Species.All`, `RecipeBook.All`,
  `Actions`, `DayTick`); Web only draws and calls `Actions`.
- New art = more `string[]` rows in `Art/Sprites.cs` using palette letters;
  new sound = code in `Audio/Sfx.cs` / `Music.cs`. No files, no pipeline.
- Save compatibility: `SaveSystem.Version` bumps whenever `GameState` gains a
  field that must not default to a wrong value; otherwise new fields with
  sensible defaults just deserialise as zero/false.
- Tune one lever, re-run the bot, read the per-level trace (a scratch console
  that references Core and copies `GreedyBot.cs` with the `Assert` line
  removed prints "level N: avg day D" — keep it outside the repo).

---

## Phase 1 — First impression (~1.5 h)

Goal: a new player understands the loop and sees money within 10 minutes.

1. **Sleep hint** — `SpiceWizardGame.Update`: when no panel is open, every
   unlocked plot is either empty, mature, or watered today, and the bucket
   has not just been refilled, toast once per day "Nothing left to do? Click
   the tower door to sleep." Track `_hintedSleepDay` on the game, not in
   `GameState`.
2. **HUD sleep button** — `Overlays.Hud`: a moon icon (`ic_moon`, new 8×8
   sprite) left of the `?` button; click → `ss.Open(PanelKind.Door)`, tooltip
   "Go to bed". Same path as clicking the door so the wizard still walks over.
3. **Starter kit** — `GameState.NewGame`: `Balance.StartingBellMash = 1`,
   `StartingBananaSeeds = 1`, and the three Golden Curry spices (Cumin,
   Coriander, Curry Leaves ×1). Day 1 can bottle Bell Hot Sauce (mash + 1 pc);
   Golden Curry follows the first harvest. Update `README` "Resources".
4. **Day length** — `Balance.DayLengthSeconds` 480 → 360. Bot is unaffected
   (it does not use the clock); check the feel in the browser with `?demo`.
5. **New-game confirm** — `Overlays.Title`: when `ss.HasSave`, "New game"
   opens a two-button confirm ("Overwrite the save?") drawn in the same box
   before calling `RequestNewGame`.
6. **Title screen text** — jam name, themes "Curry & Pepper", one credits
   line. Pure `Overlays.Title` text.

Tests: `ActionsTests` — new game can cook Bell Hot Sauce on day 1 and cannot
cook Golden Curry. Bot test still passes (starter kit shortens runs by ~1 day).

## Phase 2 — Progression curve (~2 h)

Goal: every level from 2 to 20 says something in the "Next:" tooltip; hot
sauces are worth fermenting.

1. **Re-spread unlocks** (`RecipeBook.All`, `Species.All`, `SpiceInfo`,
   `Garden.UnlockedPlots`, `FermentShelf.UnlockedJars`). Target table:

   | Lv | Unlock |
   |----|--------|
   | 2  | blends |
   | 3  | plot 5, Cardamom |
   | 4  | Bonnet seeds, Sunset Curry |
   | 5  | Bonnet Fire, jar 3, Cloves, Fenugreek |
   | 6  | plot 6 |
   | 7  | Bonnet Curry |
   | 8  | Ghost seeds |
   | 9  | plot 7 |
   | 10 | Phantom Sauce |
   | 11 | +1 spice (already), jar 4 |
   | 12 | plot 8 |
   | 13 | Spectral Curry |
   | 14 | crate 6 → 8 (`Balance.CrateCapacityAt(level)`) |
   | 15 | recipe 9 |
   | 16 | second hasten cast per day (`Balance.HastenCastsAt(level)`, `HastenedToday` → `HastensToday` int) |
   | 17 | recipe 10 |
   | 18 | aged mash in 3 nights (`Jar.NightsToAge` → `Balance.NightsToAgeAt(level)`; `Jar` needs the level, so pass it into `EmptyJar`/`Status`/`IsAged` checks via `FermentShelf.EndOfNight(level)` or store `AgeNights` on the jar when filled) |
   | 19 | boredom threshold 2 → 3 (the town forgives more) |
   | 20 | celebration |

   `Progression.UnlockAt` must learn about the new rewards (crate, hasten,
   ageing, boredom) so the tooltip and morning report list them.
2. **Two recipes** appended to `RecipeBook.All` (ids 9, 10; tier 5 falls out
   of `(Id+1)/2`):
   - 9 *Rainbow Chutney*, hot, value 85, lv 15: Bonnet mash + Banana mash +
     Ginger + Cinnamon + 2 pc.
   - 10 *Wizard's Curry*, curry, value 100, lv 17: Ghost powder + Bonnet
     powder + Cardamom + Fenugreek + Cloves + 3 pc.
   Needs two sauce icons/colours in `ItemArt` (bottle tints only, no new
   sprite shape). `Quota.Generate` and boredom already handle any id.
3. **Hot-sauce values** ×1.25: 12→15, 20→25, 32→40, 55→70. Then re-check the
   bot's sales mix; the goal is Phantom/Bonnet Fire in the same order of
   magnitude as their curries, not parity.
4. **Weekly craving** — `Quota` gains `CravedType: SauceType?` rolled in
   `Generate` (50% chance, alternating); `Town.Rate` adds +1 star for that
   type all week. Board panel shows "The town craves hot sauces this week."
   This is what pulls Banana/Bell back into rotation late.
5. **Bot** — `GreedyBot.CookEverything` should already pick up the new recipes
   (it iterates `RecipeBook.All`); confirm it ferments when hot sauces pay.
   Assert range may need widening at the top if the run lengthens; target
   median 28–34.

Tests: `TownTests` for the craving star; `ProgressionTests` (new) asserting
`UnlockAt(n)` is non-empty for every n in 2..20; bot median in range.

## Phase 3 — Second event type: rush orders (~2 h)

Goal: mornings vary; a reason to cook a specific sauce *now*.

1. Core: `RushOrder { RecipeId, Count, DueDay, PayMultiplier = 2 }` on
   `GameState.Rush` (nullable). `DayTick.Sleep` step 4½: if none is open and
   `rng.Next(100) < Balance.RushChance (35)` and level ≥ 3, roll one from
   `RecipeBook.UnlockedAt(level)` with `DueDay = day + 2`. In step 2, sauces
   matching an open rush are paid at ×2 and counted; when `Count` is met the
   order closes with `Balance.RushXp` bonus; on `DueDay` unmet, it expires
   (report line, no penalty). `MorningReport` gains `RushPosted`,
   `RushCompleted`, `RushExpired`.
2. UI: Board panel gets a "Rush order" block under the quota with the
   deadline; HUD shows a small envelope pip next to the day while one is
   open; Door panel warns "The rush order is due tonight."
3. Flavour: a `Remark` for rush sales ("The baker ran the whole way.").
4. Bot: cook the rush recipe first if it can; keeps the balance check honest.

Tests: `RushOrderTests` — posts, pays double, expires, never overlaps.

## Phase 4 — Feel: action feedback in the yard (~3 h)

Goal: every verb has a visible wizard action; level-up is a moment.

1. **Station poses** — `WizardActor.Pose(string frame, float seconds)`; new
   sprites `wizard_water` (arm out, can tilted), `wizard_stir` ×2 (ladle up
   / down), `wizard_grind`, `wizard_cheer` (arms up). `Draw` prefers the pose
   frame while its timer runs. Hooked from `Session` callbacks: `OnWatered`
   → water pose; `Sfx.Cook` → stir; `Sfx.Grind`/`Sfx.Blend` → grind; pep
   talk and level-up → cheer. Panels stay open; the pose plays behind them.
2. **Cauldron reacts** — `SceneRenderer` keeps `BrewColor` (set from
   `ItemArt.SauceColor` on cook, decays toward the default over ~20 s) and a
   `BurstTimer` that triples steam for 1.5 s; a `bottle` sprite pops up and
   arcs to the pantry on `Particles`.
3. **Level-up in the yard** — after the dawn fade lifts and before the
   Morning panel opens, if `LevelsGained > 0`: `Overlays.BigText("Level N!")`
   for 1.5 s with a `Particles.Sparkle` ring around the wizard, and delay
   `Sfx.LevelUp` to that moment instead of playing it under the fade.
4. **Actionable markers** — `SceneRenderer`: a bobbing `ic_bang` (new 5×5
   sprite) over a mature plot, a ready jar, the pantry when
   `Inventory.Sauces.Count > 0`, and the crate when it is empty after 18:00.
   One helper `Marker(x, y)`; keep it off while a panel is open.
5. **Merchant and townsfolk** — a `merchant` sprite seated on the cart; at
   dawn (`_cartTimer`) two `villager` sprites walk from the town along the
   road carrying the crate off-screen left when the crate was non-empty.
   Reuse `WizardActor` movement code via a tiny `Walker` struct.

Tests: none in Core; verify with `?demo&panel=Cauldron` screenshots and the
`?demo=night` state.

## Phase 5 — Depth for the plateau (~2 h)

1. **Tasted blends page** — `Town.TastedBlends` already holds keys; also
   store `BlendMemory { Key, Name, Stars, Pay }` on first taste (Version bump
   or lazy backfill). Board panel gains a tab "Tasted" listing them, sorted by
   stars. Mortar panel shows "Best so far: X ★★★★" next to the draft.
2. **Town favourite** — once a week (`Quota.Generate`), if any blend has ≥ 4
   stars, the notice also asks for 1× that blend at a ×1.5 bonus; `Rate`
   treats a matching `BlendKey` like a quota line. Gives blends a repeat use
   without breaking the novelty rule.
3. **Best-day record** — `Settings` gains `BestDay` (int, 0 = none),
   serialised as `best=28`; set in `SpiceWizardGame` when `BecameMaster` and
   the day beats it. Title screen shows "Best: Master on day N"; Celebration
   shows "New record!" when it was beaten.
4. **Morning report additions** — "Next unlock at level N: …" line; a
   boredom line listing recipes the town is tired of today.

Tests: `TownTests` for favourite-blend payout; settings round-trip test in
Web is not possible (no test project) — verify manually.

## Phase 6 — Tension (optional, ~1.5 h)

Only if the game still feels frictionless after Phases 1–5.

1. **Heatwave** — `Weather.Heatwave` (chance 12%, `ClearDaysAtStart` stays):
   plants not watered that day lose 1 point at night (`Plant.EndOfNight`
   takes the weather); the bucket holds its usual 4 so the well matters.
   `ic_sun_hot` icon; sky tint warmer; cicada loop in `Ambience`.
2. **Spoilage** — a jar left `NightsToAge + 3` nights turns to "Spoiled mash"
   (empties to nothing, morning report line). Blinks red on the shelf.
3. **Frost** — winter-ish variant if wanted later; same shape as Heatwave.

Tests: `WeatherTests` and `PlantTests` for the point loss and spoilage.

## Phase 7 — Sound pass (~1 h, all synth)

1. Quota-met jingle (`Sfx.Fanfare`) at the morning report; distinct from
   `Chime`.
2. Level-up sting moved out of the fade (done in Phase 4.3).
3. Night lullaby — a fourth `Music.Track` "Moth Lamp" (A minor, slow, low
   volume) that plays 20:00–22:00 and under the Door panel; `AudioMixer`
   already fades tracks by daylight, add a night slot.
4. Celebration tune (major, fast, 30 s loop) while `Crowd.Active`; crowd
   cheer noise burst on start.
5. Cat meow/purr one-shots on the cat behaviours (Cats already has
   behaviour switches to hook); cart wheels + hoof clops during `_cartTimer`.
6. Rush-order envelope "flap" and rain-on-cauldron hiss are nice-to-have.

---

## Summary schedule

| Phase | Hours | Ship line |
|-------|-------|-----------|
| 1 First impression | 1.5 | judges reach a sale in 10 min |
| 2 Progression | 2 | no empty levels; hot sauces viable |
| 3 Rush orders | 2 | mornings differ |
| 4 Yard feedback | 3 | actions look like actions; level-up is seen |
| 5 Plateau depth | 2 | blends are a collection; replay score |
| 6 Tension | 1.5 | optional |
| 7 Sound | 1 | optional |

Phases 1–2 are the minimum that fixes the review's three headline gaps;
1–4 is the recommended jam cut.

# Spice Wizard — content review (2026-09-12)

Notes only; nothing in the game was changed. Numbers below come from the code
(`Balance`, `Species.All`, `RecipeBook.All`) and from running the `GreedyBot`
over 20 seeds with a per-level trace.

## Verdict

The skeleton is complete: 4 peppers with distinct care rules, 8 recipes, 8
spices, free-form blends, weekly quotas, boredom, weather, day/night, hasten
spell, save/continue, options, 3 music tracks + ambience + voice, cats, win
celebration. That is a lot for one weekend and none of it is a stub.

What is missing is **pacing**, not systems. Three gaps matter most, in order:

1. **The last third of the game has nothing new in it.** Last unlock is at
   level 12; the win is at level 20.
2. **The first 3–4 days have no payoff.** A jam judge's 15 minutes can end
   before the first sale.
3. **Hot sauces / fermenting are dominated by curries / grinding**, so half the
   recipe list and the whole jar shelf fall out of use after level 6.

Everything else is polish.

---

## 1. Gameplay and content (heaviest weight)

### 1a. Late-game plateau — biggest gap

Bot trace (avg day the level is reached, 20 seeds):

| Lv | 2 | 4 | 6 | 8 | 10 | 12 | 14 | 16 | 18 | 20 |
|----|---|---|---|---|----|----|----|----|----|----|
| Day| 5 | 8 | 11| 15| 17 | 21 | 23 | 26 | 29 | 32 |

Unlock table: recipes at 1,1,2,3,5,6,9,11 · seeds at 4,8 · spices at 3,5,5 ·
plots at 3,6,9,12 · jars at 5,10 · blends at 2. **Nothing unlocks between level
12 and 20 except +1 max spice every 3 levels.** That is ~11 game days, a third
of the run, ~90 real minutes at 8 min/day, spent repeating Spectral/Bonnet
Curry. XP-to-next keeps rising (190 → 295) so the levels also get *slower*
exactly where they are emptiest.

Cheapest fixes (Core numbers only, bot re-check):
- **Re-spread existing unlocks** across 1–19: e.g. Phantom Sauce 9→10,
  Spectral Curry 11→13, 4th jar 10→14, 8th plot 12→16, Ghost seeds 8→9.
- **Turn existing mechanics into unlock rewards** so every level says
  something in the "Next:" tooltip: crate capacity 6→8 at 14, second hasten
  cast per day at 15, aged mash in 3 nights at 17, jar holds 3 peppers → 2
  mash at 18, boredom window 3→2 days at 19.
- **Two more recipes** in the 13–17 band (e.g. a mixed-pepper "Rainbow Chutney"
  needing two mashes, and a "Wizard's Curry" needing a blend as an ingredient
  — that would also give blends a second purpose). `Tier = (Id+1)/2` already
  handles ids 9 and 10.
- **A fifth pepper at ~14** with a new care rule (e.g. "grows only on windy
  days" or "needs a neighbour plot planted") would use the weather system as a
  mechanic, not just flavour. Needs one plant sprite set + icon.

### 1b. Early-game dead air

Start: 4 Bell seeds, 30 pc, Bell needs 3 watered nights. First possible sale is
night 4 (Golden Curry: 2 bells + 10 pc of spices); first morning report with
money is **day 5**. Bell Hot Sauce needs mash → day 6+. At 8 real minutes a
day that is 30+ minutes of "water, wait" unless the player finds the door.

- The clock already pauses in panels and the door sleeps early, so the fix is
  **teaching**, not balance: a toast when every plot is watered ("Nothing left
  to do? Click the tower door to sleep."), or a small bed/moon button in the
  HUD that does what the door does.
- Consider a **starter kit** that lets day 1 end with something in the crate:
  1 Bell mash + 1 pc, or 2 Bell peppers + the three Golden Curry spices. Day-1
  cook → day-2 report → the loop is understood in 10 minutes.
- Start with **1 Banana seed** too, so the "each pepper is different" idea
  shows up on day 1 rather than after the first purchase.
- Optional: `DayLengthSeconds` 480 is generous for a jam build. 300–360 with
  the same pause-in-panels rule would still feel unhurried.

### 1c. Hot sauces and the jar shelf fall out of use

Bot sales over 20 runs: Spectral Curry 330, Bonnet Curry 290, Golden Curry 120,
blends 82, Bell Hot Sauce 80, Bonnet Fire 54, Phantom Sauce 8, **Banana Blaze
0, Sunset Curry 0**.

Why: a hot sauce costs 2 peppers + 2 nights + a jar slot; the same-tier curry
costs 1 pepper + 1 spice of grinding and is worth more (Bonnet Fire 32 vs
Bonnet Curry 40; Phantom 55 vs Spectral 70). Banana has no role after Bonnet
unlocks at level 4–6.

- Raise hot-sauce base values ~25% (Bell 12→15, Banana 20→25, Bonnet 32→40,
  Phantom 55→70) or make aged mash worth +2 stars. Fermenting should be the
  slow-but-better path, not the slow-and-worse one.
- Give quotas a bias toward the *lower* half of the unlocked pool, or a
  weekly "town craving" (one sauce type gets +1 star all week) so the shelf
  and the older peppers come back around.
- Cap the boredom penalty harder at the top (e.g. threshold 2 → 1 for tier 4)
  so the endgame is not "alternate two curries".

### 1d. Nothing can go wrong

Plants never wilt, mash never spoils, weather only ever helps. Cozy is a
legitimate choice, but a *little* tension makes decisions matter:
- Jars over-aged past N nights spoil (or lose the aged bonus).
- A plant unwatered 3 days running drops back a growth point.
- Rain is only good; a **Heatwave** day (`Weather` enum + one icon) that dries
  plants unless watered twice, or a **Frost** that stalls growth, would make
  the morning weather line something to react to.

### 1e. Only one event type

The weekly quota is the only thing that changes week to week. Cheap additions
in `DayTick`/`MorningReport`:
- **Rush orders**: "The baker wants 1 Banana Blaze by tomorrow night, double
  pay." A single line, 2-day deadline, chance per morning.
- **Market days**: merchant discounts one spice, or offers a rare spice for
  one day only.
- **Festival** on day 7 of every other week: all fame ×2 that night, crowd
  sprites on the road.
- A cat occasionally drops a seed at the door (they are already there and
  purely decorative).

### 1f. Blends are deep but undocumented in-game

Novelty is the whole reward and the player has no list of what the town has
tasted. A **"Tasted" page** on the notice board (blend name, stars, pay) turns
blends into a collection meta-game for the plateau. Also consider a "town
favourite" — the best-rated blend gets requested once a week for a bonus.

### 1g. Post-win and replay

"Keep playing" changes nothing. Add a **best day** saved with settings, shown
on the title screen ("Best: Master on day 28"), and put the day count on the
celebration as the score. For a jam that is the whole replay hook.

---

## 2. UI / onboarding

- **First-day checklist** in place of (or in front of) the Help panel:
  Plant → Water → Sleep → Harvest → Cook → Ship → Read the report, with ticks
  that fill in as the player does them. This is the single biggest win for
  judges who will not read a wall of text.
- **"Something to do here" markers**: a small `!` over a ripe plot, a ready
  jar, a pantry with unshipped sauces, an empty crate after 18:00. The scene
  already blinks ready jars and shows the crate count; make the rule uniform.
- **HUD**: a sleep button (see 1b), and a day-of-week / "quota due in N days"
  pip next to the day counter.
- **Morning report**: show "next unlock at level N: …" so the plateau at least
  has a visible target; show the boredom state ("the town is tired of X").
- **New game** should confirm when a save exists.
- **Title screen**: jam name + themes ("Curry" and "Pepper"), a credits line,
  and the best-day record.
- Pantry sauce list truncates at 6 ("+N more") — a scroll or two columns.

## 3. Art / animation

Existing: wizard walk (2 frames), idle hat-flop, blink, pipe-smoking idle,
back view, door in/out, cart trundle, cats (very complete), particles for
water/sparkle/steam/smoke/leaves/confetti/rain, wind sway, day/night haze,
crowd at the win. Coverage is high; gaps are all **action feedback**:

- **Station-use poses**: watering (arm out, can tilt), stirring the cauldron,
  grinding at the mortar, a pep-talk gesture (arms up). Even one 2-frame pose
  per verb, shown for ~0.6 s after the button, would sell every action more
  than the particles do.
- **Cauldron reacts to cooking**: brew colour takes the last sauce's colour, a
  bottle pops out, a bigger steam burst. Right now steam is constant.
- **Level-up in the yard**: the sound plays under the black fade so it is
  buried. A sparkle ring + "Level 7!" big text when the fade lifts.
- **Merchant NPC** on the cart, and 1–2 townsfolk walking the road at dawn
  carrying the crate away — the town is currently only skyline.
- **Plant states** for the tension mechanics in 1d if added (wilted / frosted
  frames).
- Sky: a moon phase or stars twinkle at night; sunset colour band at 20:00+.

## 4. Sound

Existing: 3 daytime tracks, wind/birds/crickets/rain/cauldron loops, ~25 SFX
incl. footsteps, formant voice mumbles. Missing, roughly in value order:

- **Level-up sting audible outside the fade**, and a **quota-met jingle**
  distinct from the morning chime.
- **Night music** — a short lullaby/pad for the 20:00–22:00 stretch instead of
  silence + crickets, and a **celebration tune** for the win.
- Cart wheels/horse clop at dawn; crowd cheer under the confetti.
- Cat meow/purr on the cat behaviours (they are silent).
- Rain-on-cauldron sizzle, jar "pop" when a ferment finishes overnight.

## 5. Suggested order for a final jam pass

1. Sleep hint/button + starter kit (30 min; fixes the first impression).
2. Re-spread unlocks + hot-sauce values, re-run `dotnet test` (1 h; fixes the
   plateau).
3. Rush orders (2 h; the one new system that pays off most per hour).
4. Station-use poses + cauldron reaction + visible level-up (2–3 h).
5. Blend "Tasted" list and best-day record (1–2 h).
6. Night lullaby / quota jingle / cat sounds (1 h, all code-synth).

Remember `instructions/general-instructions.md`: README must be updated for
any of these, and the bot test asserts median 20–40 days.

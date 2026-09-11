# The King's Chef

An arcade-style roguelike built for a weekend game jam around the themes **Curry** and **Pepper**.

You are the King's chef. Dinner is late, the castle is full of guests, and the guests are chess pieces. They know the rules; you don't. So you move like a roguelike hero — one tile in any of eight directions — while the guests slide, jump and poke you exactly the way chess pieces would. Gather ingredients on the way down, survive the guests' forks, insults and demands for snacks, find the King in the Dining Hall and serve him whatever you managed to keep in your pantry.

## Play

```bash
npm install
npm run dev      # opens a local dev server
```

Open the printed URL in a browser. Add `?seed=123` to replay a specific castle layout.

The dev and preview servers bind to `127.0.0.1` only (see [vite.config.js](vite.config.js)), so the game is reachable solely from this machine and is never exposed on the local network. Ports are fixed (`5173` dev, `4173` preview) and the server fails fast rather than hopping ports if one is busy.

## Controls

| Action | Keys |
| --- | --- |
| Move / bump (8 directions) | Arrows, `WASD` (+ `QEZC` diagonals), numpad, or vi-keys `hjklyubn` |
| Wait a turn | `Space`, `.`, numpad `5` |
| Hold a pantry ingredient to serve | `1`–`8` (press again to put it down), `Esc` to put it down |
| New run | `R` |
| Mouse | Click a lit tile to walk one step toward it; click an adjacent guest to bump it; click a pantry slot to hold it |

## Rules

| Thing | Behaviour |
| --- | --- |
| **The chef** | Steps one tile in any direction. Bumping a guest whacks it with your utensil. Bumping the King serves dinner. |
| **Patience** | Your health (12). Guests wear it down with jabs, insults and demands. At zero you tear off your apron and storm out. |
| **Fog of war** | You see 7 tiles with line of sight; explored rooms stay on the map, dimmed. Guests only act once they've noticed you (you see them, or you clatter within earshot) and skip the turn they notice you on. |
| **Threat overlay** | Red tissue marks every tile a guest that knows you're there could hit next turn. Guests attack *from where they stand* along their chess lines — they don't need to reach you. |
| **Utensil** | Your attack. Wooden spoon (1) → rolling pin (2) → cleaver (3). Better ones swap in automatically. |
| **Apron** | Your armour. Each leather apron pads jabs by 1 (max 3). Insults ignore aprons entirely. |
| **Pantry** | Holds 8 ingredients. Everything in it ends up on the King's plate — unless you feed some to a guest, or a Baron snatches it. |
| **Serving a guest** | Hold an ingredient (`1`–`8`) and bump any guest: they leave delighted, worth their tips +10. Instant, safe, and it works on the Duchess. |
| **Gear** | Tea (+5 patience), utensils and aprons are used the moment you step on them. |
| **Stairs** | Step on them to descend (+25 tips). Three floors down is the Dining Hall. |
| **The King** | Bump him to serve. The meal is judged on what's in your pantry. |

### The guests

| Guest | Piece | Moves | Attack |
| --- | --- | --- | --- |
| **The Peckish Page** | pawn | steps straight, pokes diagonally | jab, 1 · 1 hp |
| **Sir Ladle** | knight | jumps in an L, even over walls; can't hit anyone adjacent | jab, 2 · 2 hp · winded for a turn after each hit |
| **The Bishop of Basil** | bishop | slides diagonally | insult, 1 · 2 hp |
| **Baron Peppermill** | rook | slides straight | demands food: snatches an ingredient and leaves with it, or jabs for 1 if your pantry is empty · 3 hp |
| **Duchess Toque** | queen | slides any direction | insult, 2 · 4 hp · pauses for breath between tirades |

Each turn a guest attacks if it can, otherwise moves to a square from which it could, otherwise closes in. Closest guests act first.

### The meal

The King has ordered a **Royal Curry**: chili pepper, curry paste, bowl of rice, chicken leg, onion. Bread rolls are the only other ingredient and count for tips only. Recipe parts are dealt across the floors so a full curry is always possible.

| Plate | Rank | Tips |
| --- | --- | --- |
| Every recipe part | **S** | ingredients + 5 × 20 + 100 |
| 4 parts | A | ingredients + parts × 20 |
| 2–3 parts | B | ingredients + parts × 20 |
| Something, but not much | C | ingredients |
| Nothing | F | shown to the moat (run lost) |

Other tips: +5 (page) to +30 (duchess) for sending a guest packing, +10 on top of that for feeding one, +25 per floor, +5 for surplus gear. Your best score is remembered in the browser.

### Floors

1. The Cellar — 3 pages, 1 knight
2. The Kitchens — 3 pages, 1 knight, 2 bishops · rolling pin
3. The Great Hall — 2 pages, 1 knight, 1 bishop, 2 barons · leather apron
4. The Dining Hall — 2 pages, 1 knight, 2 bishops, 1 baron, the Duchess · cleaver · the King

Every floor is a seeded rooms-and-corridors castle (30×18). You start alone in a room and are never in a guest's line of fire at the top of the stairs; the stairs (or the King) are in the room farthest from you. Each floor carries three ingredients and a pot of tea.

## Art direction

**Paper cut-out**: flat SVG silhouettes on cream/charcoal "paper" with hard offset shadows and a stable per-tile rotation jitter, on a kraft-and-wood cutting board. Guests are kitchen-twisted chess pieces (page = pawn, knight = ladle, bishop = spice jar, rook = pepper grinder, queen = chef's toque); the chef is a pawn in a toque with a chili pinned to the band, the King wears a gold crown. Floors are paper chess squares, walls are dark paper blocks, unexplored castle is bare kraft. Markers: red tissue = threatened, orange ring = guest you can bump, gold ring = the King, cream strips = speech bubbles.

All art is inline SVG in [src/art/sprites.js](src/art/sprites.js) (colours from CSS variables in [src/style.css](src/style.css)); there are no image assets. The heading font is Bree Serif from Google Fonts with a Georgia fallback — the only network request the page makes.

The piece sheet and the unchosen style explorations live on a design canvas: https://claude.ai/code/artifact/d72c6a09-f8b3-4a13-86a9-62024a593e7c

## Development

```bash
npm test         # vitest unit tests (game logic)
npm run build    # production build to dist/
npm run preview  # serve the production build
```

### Project layout

```
index.html            page shell + HUD markup
src/main.js           keyboard/mouse wiring, best-score storage, ?seed= handling
src/render.js         draws the castle floor + side panel from state
src/art/sprites.js    inline-SVG paper cut-out sprites (chef, guests, King, items, stairs) + tilt hash
src/style.css         paper palette, grain texture, board and HUD styling
src/game/constants.js map size, enums (tiles, guests, items, status), tuning numbers
src/game/rng.js       seedable PRNG
src/game/dungeon.js   rooms-and-corridors generator
src/game/fov.js       line of sight, visible set, BFS distance maps, path steps
src/game/guests.js    guest roster: stats, attack kinds, insults
src/game/items.js     ingredients, gear, the Royal Curry recipe, meal scoring
src/game/moves.js     chef moves, guest chess moves inside the dungeon, threat sets
src/game/ai.js        guest decision making (attack > line up > close in > hold)
src/game/levels.js    floor roster + seeded floor generation
src/game/state.js     pure game reducer: newGame / stepPlayer / waitTurn / selectSlot / stepToward
tests/                vitest suites for moves+fov, state, sprites
```

The game logic under `src/game/` is pure and DOM-free, so it can be tested, fuzzed and tuned without a browser.

## Tech

Vanilla JavaScript (ES modules), [Vite](https://vitejs.dev) for dev/build, [Vitest](https://vitest.dev) for tests. No runtime dependencies.

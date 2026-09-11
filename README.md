# Pepper the Pawn

A tiny turn-based chess roguelike built for a weekend game jam around the themes **Curry** and **Pepper**.

You are a single spicy pawn crossing an 8×8 kitchen board. Reach the top rank to promote and clear the level. Enemies are real chess pieces that move with real chess rules, and they move after you.

## Play

```bash
npm install
npm run dev      # opens a local dev server
```

Open the printed URL in a browser.

The dev and preview servers bind to `127.0.0.1` only (see [vite.config.js](vite.config.js)), so the game is reachable solely from this machine and is never exposed on the local network. Ports are fixed (`5173` dev, `4173` preview) and the server fails fast rather than hopping ports if one is busy.

## Rules

| Thing | Behaviour |
| --- | --- |
| **Your pawn** | Moves forward 1 (or 2 from the home rank). Captures diagonally forward. |
| **Promotion** | Reach rank 8 to clear the level (+50, +1 heart). Clear all 5 levels to win. |
| **Enemies** | Pawn, Knight, Bishop, Rook, Queen — real chess movement. Enemy pawns move *down* the board. Each turn an enemy captures you if it can, otherwise it steps toward you. Closest enemies act first. |
| **Threat overlay** | Red squares are attacked by at least one enemy right now. |
| **Getting caught** | Costs a heart and knocks you back to your home rank. Only one enemy can catch you per turn. Three hearts total. |
| 🌶️ **Pepper** | Pickup. Arm it (button or key `1`) and your next move is a **knight jump**. Can capture. |
| 🍛 **Curry** | Pickup. Arm it (button or key `2`) and your next move is a **king step** in any direction, including backwards. Can capture. |
| **Pouch** | Holds up to 3 of each pickup; extras are eaten for +5. |
| **Score** | +10 per capture, +50 per promotion, +5 per surplus pickup. |

Keyboard: `1` / `2` arm a power, `Esc` disarms, `Enter` / `Space` continues after a promotion, `R` restarts the run.

### Levels

1. The Pantry — 2 pawns, 1 knight
2. The Prep Line — 3 pawns, 1 knight, 1 bishop
3. The Stove Top — 2 pawns, 2 knights, 1 bishop, 1 rook
4. The Spice Rack — 3 pawns, 1 knight, 2 bishops, 1 rook
5. The Head Chef's Table — 2 pawns, 2 knights, 1 bishop, 1 rook, 1 queen

Layouts are seeded per run; the starting square is never under attack.

## Art direction

Settled on **paper cut-out**: flat SVG silhouettes on cream/charcoal "paper" with hard offset shadows and a stable per-square rotation jitter, on a kraft-and-wood board. Pieces are kitchen-twisted (knight = ladle, bishop = spice jar, rook = pepper grinder, queen = chef's toque; the player pawn wears a chili leaf). Board markers: green paper dot = legal move, orange ring = capture, red tissue square = attacked, gold strip = promotion rank.

The game renders this style directly: all art is inline SVG in [src/art/sprites.js](src/art/sprites.js) (colours come from CSS variables in [src/style.css](src/style.css)), so there are no image assets to load. The heading font is Bree Serif from Google Fonts with a Georgia fallback — the only network request the page makes.

The board mock-up, full piece/marker sheet and the five unchosen style explorations live on a design canvas: https://claude.ai/code/artifact/d72c6a09-f8b3-4a13-86a9-62024a593e7c

## Development

```bash
npm test         # vitest unit tests (game logic)
npm run build    # production build to dist/
npm run preview  # serve the production build
```

### Project layout

```
index.html            page shell
src/main.js           wires DOM events to the game state
src/render.js         draws board + side panel from state
src/art/sprites.js    inline-SVG paper cut-out sprites (pieces, pickups, hearts) + tilt hash
src/style.css         paper palette, grain texture, board and HUD styling
src/game/constants.js board size, piece/pickup/mode/status enums, glyphs
src/game/board.js     coordinate + lookup helpers
src/game/moves.js     player + enemy move generation, threat map
src/game/ai.js        enemy decision making (capture > approach > hold)
src/game/levels.js    level roster + seeded layout generation
src/game/state.js     pure game reducer: newGame / setMode / movePlayer / nextLevel
src/game/rng.js       seedable PRNG
tests/                vitest suites for moves, state and sprites
```

The game logic under `src/game/` is pure and DOM-free, so it can be tested and tuned without a browser.

## Tech

Vanilla JavaScript (ES modules), [Vite](https://vitejs.dev) for dev/build, [Vitest](https://vitest.dev) for tests. No runtime dependencies.

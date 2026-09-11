import { describe, it, expect } from 'vitest';
import { newGame, stepPlayer, waitTurn, selectSlot, stepToward, legalMoves } from '../src/game/state.js';
import { generateFloor, FLOORS, ingredientsFor } from '../src/game/levels.js';
import { threatenedSquares } from '../src/game/moves.js';
import { distanceMap, idx, key } from '../src/game/fov.js';
import { createRng } from '../src/game/rng.js';
import { scoreMeal, ROYAL_RECIPE, PART_TIPS, COMPLETE_TIPS, INGREDIENTS } from '../src/game/items.js';
import { GUESTS } from '../src/game/guests.js';
import { GUEST, ITEM, STATUS, MAX_PATIENCE, PANTRY_SIZE, MAP_W, MAP_H, TILE, FLOOR_TIPS } from '../src/game/constants.js';

const ROOM = [
  '#########',
  '#.......#',
  '#.......#',
  '#.......#',
  '#.......#',
  '#.......#',
  '#.......#',
  '#.......#',
  '#########',
];

/** Hand-built state so tests don't depend on floor generation. */
function scenario(overrides = {}) {
  const map = { w: ROOM[0].length, h: ROOM.length, tiles: ROOM.slice() };
  return {
    seed: 1,
    floor: 0,
    turn: 0,
    status: STATUS.PLAYING,
    tips: 0,
    patience: MAX_PATIENCE,
    maxPatience: MAX_PATIENCE,
    utensil: { name: 'wooden spoon', dmg: 1 },
    armor: 0,
    pantry: [],
    held: null,
    recipe: ROYAL_RECIPE.parts.slice(),
    log: [],
    bubbles: [],
    ending: null,
    map,
    seen: new Array(map.w * map.h).fill(1),
    player: { x: 4, y: 4 },
    guests: [],
    items: [],
    stairs: null,
    king: null,
    ...overrides,
  };
}

const guest = (type, x, y, extra = {}) => ({ id: x * 100 + y, type, x, y, hp: GUESTS[type].hp, aware: true, noticedOn: -1, ...extra });
const logText = (s) => s.log.map((l) => l.text).join(' | ');

describe('floor generation', () => {
  it('is deterministic, connected, and never starts the chef under threat', () => {
    for (let floor = 0; floor < FLOORS.length; floor++) {
      for (let seed = 1; seed <= 15; seed++) {
        const a = generateFloor(floor, seed);
        const b = generateFloor(floor, seed);
        expect(a).toEqual(b);
        expect(a.map.w).toBe(MAP_W);
        expect(a.map.h).toBe(MAP_H);

        const expected = Object.values(FLOORS[floor].guests).reduce((s, n) => s + n, 0);
        expect(a.guests).toHaveLength(expected);
        expect(a.guests.every((g) => !g.aware)).toBe(true);
        expect(threatenedSquares(a, a.guests).has(key(a.player.x, a.player.y))).toBe(false);

        // Everything sits on floor, nothing overlaps, and the goal is reachable.
        const dist = distanceMap(a.map, a.player);
        const things = [...a.guests, ...a.items, a.stairs, a.king].filter(Boolean);
        for (const t of things) expect(dist[idx(a.map, t.x, t.y)]).toBeGreaterThan(0);
        expect(new Set(things.map((t) => key(t.x, t.y))).size).toBe(things.length);

        if (FLOORS[floor].king) {
          expect(a.king).not.toBeNull();
          expect(a.stairs).toBeNull();
        } else {
          expect(a.king).toBeNull();
          expect(a.map.tiles[a.stairs.y][a.stairs.x]).toBe(TILE.STAIRS);
        }
      }
    }
  });

  it('deals every recipe part across the run', () => {
    const rng = createRng(3);
    const dealt = new Set();
    for (let floor = 0; floor < FLOORS.length; floor++) {
      for (const type of ingredientsFor(floor, rng)) dealt.add(type);
    }
    for (const part of ROYAL_RECIPE.parts) expect(dealt.has(part)).toBe(true);
  });
});

describe('newGame', () => {
  it('starts on floor 1 with full patience, an empty pantry and the King\'s order', () => {
    const s = newGame(42);
    expect(s.floor).toBe(0);
    expect(s.patience).toBe(MAX_PATIENCE);
    expect(s.pantry).toEqual([]);
    expect(s.recipe).toEqual(ROYAL_RECIPE.parts);
    expect(s.status).toBe(STATUS.PLAYING);
    expect(legalMoves(s).length).toBeGreaterThan(0);
    expect(logText(s)).toContain('Royal Curry');
    // The start room is lit.
    expect(s.seen[idx(s.map, s.player.x, s.player.y)]).toBe(1);
  });
});

describe('moving', () => {
  it('ignores walls and does not spend a turn', () => {
    const s = scenario({ player: { x: 1, y: 1 } });
    const next = stepPlayer(s, -1, 0);
    expect(next).toBe(s);
    expect(next.turn).toBe(0);
  });

  it('steps diagonally and advances the turn', () => {
    const next = stepPlayer(scenario(), 1, 1);
    expect(next.player).toEqual({ x: 5, y: 5 });
    expect(next.turn).toBe(1);
  });

  it('waiting passes the turn', () => {
    const next = waitTurn(scenario());
    expect(next.turn).toBe(1);
    expect(next.player).toEqual({ x: 4, y: 4 });
  });

  it('walks one step toward a clicked tile along known floor', () => {
    const next = stepToward(scenario(), 7, 7);
    expect(next.turn).toBe(1);
    expect(next.player).toEqual({ x: 5, y: 5 });
    // Unknown tiles are not walkable.
    const blind = scenario();
    blind.seen.fill(0);
    expect(stepToward(blind, 7, 7)).toBe(blind);
  });
});

describe('picking things up', () => {
  it('pockets ingredients and flags recipe parts', () => {
    const s = scenario({ items: [{ type: ITEM.CHILI, x: 5, y: 4 }] });
    const next = stepPlayer(s, 1, 0);
    expect(next.pantry).toEqual([ITEM.CHILI]);
    expect(next.items).toEqual([]);
    expect(logText(next)).toContain('King asked for this');
  });

  it('leaves ingredients on the floor when the pantry is full', () => {
    const s = scenario({ pantry: Array(PANTRY_SIZE).fill(ITEM.BREAD), items: [{ type: ITEM.CHILI, x: 5, y: 4 }] });
    const next = stepPlayer(s, 1, 0);
    expect(next.pantry).toHaveLength(PANTRY_SIZE);
    expect(next.items).toHaveLength(1);
  });

  it('uses gear on the spot: tea heals, better utensils replace, aprons pad', () => {
    let s = scenario({ patience: 3, items: [
      { type: ITEM.TEA, x: 5, y: 4 },
      { type: ITEM.CLEAVER, x: 6, y: 4 },
      { type: ITEM.ROLLING_PIN, x: 7, y: 4 },
      { type: ITEM.APRON, x: 7, y: 5 },
    ] });
    s = stepPlayer(s, 1, 0);
    expect(s.patience).toBe(8);
    s = stepPlayer(s, 1, 0);
    expect(s.utensil).toEqual({ name: 'cleaver', dmg: 3 });
    s = stepPlayer(s, 1, 0);
    expect(s.utensil.dmg).toBe(3); // the pin is worse; sold instead
    expect(s.tips).toBe(5);
    s = stepPlayer(s, 0, 1);
    expect(s.armor).toBe(1);
    expect(s.items).toEqual([]);
  });
});

describe('guests', () => {
  it('bumping a guest whacks it; enough whacks send it packing for tips', () => {
    const s = scenario({ guests: [guest(GUEST.KNIGHT, 5, 4)] });
    const once = stepPlayer(s, 1, 0);
    expect(once.guests[0].hp).toBe(1);
    expect(once.player).toEqual({ x: 4, y: 4 });
    expect(once.guests).toHaveLength(1);
    // A page goes down in one whack.
    const twice = stepPlayer(scenario({ guests: [guest(GUEST.PAGE, 5, 4)] }), 1, 0);
    expect(twice.guests).toEqual([]);
    expect(twice.tips).toBe(GUESTS[GUEST.PAGE].tips);
  });

  it('a knight is winded for a turn after landing a hit', () => {
    const s = scenario({ guests: [guest(GUEST.KNIGHT, 6, 5)] });
    const hit = waitTurn(s);
    expect(hit.patience).toBe(MAX_PATIENCE - GUESTS[GUEST.KNIGHT].dmg);
    const rest = waitTurn(hit);
    expect(rest.patience).toBe(hit.patience);
    expect(rest.bubbles.some((b) => b.text.startsWith('huff'))).toBe(true);
    const again = waitTurn(rest);
    expect(again.patience).toBe(hit.patience - GUESTS[GUEST.KNIGHT].dmg);
  });

  it('a page pokes diagonally but is harmless straight on', () => {
    const diag = waitTurn(scenario({ guests: [guest(GUEST.PAGE, 5, 5)] }));
    expect(diag.patience).toBe(MAX_PATIENCE - 1);
    expect(logText(diag)).toContain('Peckish Page');
  });

  it('the apron softens jabs but not insults', () => {
    const jab = waitTurn(scenario({ armor: 1, guests: [guest(GUEST.PAGE, 5, 5)] }));
    expect(jab.patience).toBe(MAX_PATIENCE);
    expect(logText(jab)).toContain('apron takes it');

    const sneer = waitTurn(scenario({ armor: 3, guests: [guest(GUEST.BISHOP, 7, 7)] }));
    expect(sneer.patience).toBe(MAX_PATIENCE - GUESTS[GUEST.BISHOP].dmg);
    expect(sneer.bubbles.some((b) => b.x === 7 && b.y === 7)).toBe(true);
  });

  it('the Baron snatches food from a stocked pantry, otherwise jabs', () => {
    const stocked = waitTurn(scenario({ pantry: [ITEM.RICE], held: 0, guests: [guest(GUEST.ROOK, 4, 1)] }));
    expect(stocked.pantry).toEqual([]);
    expect(stocked.held).toBeNull();
    expect(stocked.patience).toBe(MAX_PATIENCE);
    expect(stocked.guests).toEqual([]); // satisfied, he leaves
    expect(stocked.tips).toBe(0);
    expect(logText(stocked)).toContain('snatches your bowl of rice');

    const bare = waitTurn(scenario({ guests: [guest(GUEST.ROOK, 4, 1)] }));
    expect(bare.patience).toBe(MAX_PATIENCE - 1);
  });

  it('serving a held ingredient satisfies any guest', () => {
    const s = scenario({ pantry: [ITEM.BREAD, ITEM.CHILI], guests: [guest(GUEST.QUEEN, 5, 4)] });
    const holding = selectSlot(s, 1);
    expect(holding.held).toBe(1);
    expect(holding.turn).toBe(0); // free action
    const fed = stepPlayer(holding, 1, 0);
    expect(fed.guests).toEqual([]);
    expect(fed.pantry).toEqual([ITEM.BREAD]);
    expect(fed.held).toBeNull();
    expect(fed.tips).toBe(GUESTS[GUEST.QUEEN].tips + 10);
    expect(logText(fed)).toContain('delighted');
  });

  it('selectSlot toggles, clears with null, and rejects empty slots', () => {
    const s = scenario({ pantry: [ITEM.BREAD] });
    expect(selectSlot(s, 3)).toBe(s);
    const held = selectSlot(s, 0);
    expect(selectSlot(held, 0).held).toBeNull();
    expect(selectSlot(held, null).held).toBeNull();
  });

  it('unaware guests do nothing until they notice you, then skip that turn', () => {
    // Bishop in line with the chef but not yet aware (e.g. was behind a door).
    const s = scenario({ guests: [guest(GUEST.BISHOP, 7, 7, { aware: false })] });
    const t1 = waitTurn(s);
    expect(t1.guests[0].aware).toBe(true);
    expect(t1.patience).toBe(MAX_PATIENCE);
    expect(logText(t1)).toContain('notices you');
    const t2 = waitTurn(t1);
    expect(t2.patience).toBe(MAX_PATIENCE - 1);
  });

  it('guests approach when they cannot attack', () => {
    const s = scenario({ guests: [guest(GUEST.PAGE, 1, 4)] });
    const next = waitTurn(s);
    expect(next.guests[0].x).toBe(2);
  });

  it('running out of patience ends the run', () => {
    const s = scenario({ patience: 1, guests: [guest(GUEST.PAGE, 5, 5)] });
    const next = waitTurn(s);
    expect(next.status).toBe(STATUS.LOST);
    expect(next.patience).toBe(0);
    expect(waitTurn(next)).toBe(next);
    expect(stepPlayer(next, 1, 0)).toBe(next);
  });
});

describe('floors and the King', () => {
  it('stepping onto the stairs loads the next floor and pays tips', () => {
    const rows = ROOM.map((r, y) => (y === 4 ? r.slice(0, 5) + TILE.STAIRS + r.slice(6) : r));
    const s = scenario({ seed: 7, map: { w: 9, h: 9, tiles: rows }, stairs: { x: 5, y: 4 } });
    const next = stepPlayer(s, 1, 0);
    expect(next.floor).toBe(1);
    expect(next.tips).toBe(FLOOR_TIPS);
    expect(next.map.w).toBe(MAP_W);
    expect(next.turn).toBe(1);
    expect(logText(next)).toContain(FLOORS[1].name);
  });

  it('serving the King with the full recipe wins with rank S', () => {
    const s = scenario({ floor: FLOORS.length - 1, pantry: [...ROYAL_RECIPE.parts, ITEM.BREAD], king: { x: 5, y: 4 } });
    const next = stepPlayer(s, 1, 0);
    expect(next.status).toBe(STATUS.WON);
    expect(next.ending.rank).toBe('S');
    expect(next.ending.complete).toBe(true);
    expect(next.tips).toBe(next.ending.tips);
    expect(next.tips).toBe(5 * 10 + 5 + 5 * PART_TIPS + COMPLETE_TIPS);
  });

  it('serving the King an empty plate loses', () => {
    const next = stepPlayer(scenario({ king: { x: 5, y: 4 } }), 1, 0);
    expect(next.status).toBe(STATUS.LOST);
    expect(next.ending.rank).toBe('F');
  });
});

describe('scoreMeal', () => {
  it('ranks by recipe coverage', () => {
    expect(scoreMeal([]).rank).toBe('F');
    expect(scoreMeal([ITEM.BREAD]).rank).toBe('C');
    expect(scoreMeal([ITEM.CHILI, ITEM.RICE]).rank).toBe('B');
    expect(scoreMeal([ITEM.CHILI, ITEM.RICE, ITEM.CURRY, ITEM.ONION]).rank).toBe('A');
    expect(scoreMeal(ROYAL_RECIPE.parts).rank).toBe('S');
    expect(scoreMeal([ITEM.CHILI, ITEM.CHILI]).parts).toEqual([ITEM.CHILI]);
    expect(scoreMeal([ITEM.BREAD]).tips).toBe(INGREDIENTS[ITEM.BREAD].tips);
  });
});

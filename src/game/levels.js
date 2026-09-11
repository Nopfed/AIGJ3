import { MAP_W, MAP_H, GUEST, ITEM, TILE } from './constants.js';
import { createRng } from './rng.js';
import { generateDungeon, center, roomTiles } from './dungeon.js';
import { chebyshev, key } from './fov.js';
import { threatenedSquares } from './moves.js';
import { guestDef } from './guests.js';
import { INGREDIENTS, ROYAL_RECIPE } from './items.js';

// Floor roster. The King waits on the last one.
export const FLOORS = [
  { name: 'The Cellar', guests: { [GUEST.PAGE]: 3, [GUEST.KNIGHT]: 1 }, gear: [ITEM.TEA] },
  { name: 'The Kitchens', guests: { [GUEST.PAGE]: 3, [GUEST.KNIGHT]: 1, [GUEST.BISHOP]: 2 }, gear: [ITEM.TEA, ITEM.ROLLING_PIN] },
  { name: 'The Great Hall', guests: { [GUEST.PAGE]: 2, [GUEST.KNIGHT]: 1, [GUEST.BISHOP]: 1, [GUEST.ROOK]: 2 }, gear: [ITEM.TEA, ITEM.APRON, ITEM.TEA] },
  { name: 'The Dining Hall', guests: { [GUEST.PAGE]: 2, [GUEST.KNIGHT]: 1, [GUEST.BISHOP]: 2, [GUEST.ROOK]: 1, [GUEST.QUEEN]: 1 }, gear: [ITEM.TEA, ITEM.CLEAVER, ITEM.TEA], king: true },
];

const MAX_ATTEMPTS = 100;
const MIN_START_GAP = 3; // no guest within this many tiles of the start

/**
 * Recipe parts are dealt round-robin across floors so a full Royal Curry is
 * always possible in one run; each floor also gets one random extra ingredient.
 */
export function ingredientsFor(floorIndex, rng) {
  const parts = ROYAL_RECIPE.parts;
  const first = (floorIndex * 2) % parts.length;
  return [parts[first], parts[(first + 1) % parts.length], rng.pick(Object.keys(INGREDIENTS))];
}

/**
 * Build one floor: dungeon, chef start, guests, items, and either the stairs
 * down or the King. The start tile is never threatened by any guest.
 */
export function generateFloor(floorIndex, seed) {
  const def = FLOORS[floorIndex];
  if (!def) throw new Error(`No floor ${floorIndex}`);
  const rng = createRng(seed * 7919 + floorIndex * 104729);

  for (let attempt = 0; attempt < MAX_ATTEMPTS; attempt++) {
    const layout = tryLayout(def, floorIndex, rng);
    if (!layout) continue;
    const threats = threatenedSquares(layout, layout.guests);
    if (!threats.has(key(layout.player.x, layout.player.y))) return layout;
  }
  throw new Error('Could not generate a safe floor layout');
}

function tryLayout(def, floorIndex, rng) {
  const dungeon = generateDungeon(rng, MAP_W, MAP_H);
  const { rooms } = dungeon;
  const startRoom = rooms[0];
  const player = center(startRoom);

  // The goal room is the one farthest from the start.
  let farRoom = rooms[1];
  for (const room of rooms.slice(1)) {
    if (chebyshev(center(room), player) > chebyshev(center(farRoom), player)) farRoom = room;
  }

  const taken = new Set([key(player.x, player.y)]);
  const claim = (p) => {
    taken.add(key(p.x, p.y));
    return p;
  };

  let king = null;
  let stairs = null;
  const tiles = dungeon.tiles.map((row) => row.split(''));
  if (def.king) {
    king = claim(center(farRoom));
  } else {
    stairs = claim(rng.pick(roomTiles(farRoom).filter((t) => !taken.has(key(t.x, t.y)))));
    tiles[stairs.y][stairs.x] = TILE.STAIRS;
  }
  const map = { w: dungeon.w, h: dungeon.h, tiles: tiles.map((row) => row.join('')) };

  // Guests wait in every room but the chef's, well away from the start.
  const guestSpots = rng.shuffle(
    rooms.slice(1).flatMap(roomTiles).filter((t) => !taken.has(key(t.x, t.y)) && chebyshev(t, player) >= MIN_START_GAP),
  );
  const guests = [];
  let id = 0;
  for (const [type, count] of Object.entries(def.guests)) {
    for (let i = 0; i < count; i++) {
      const spot = guestSpots.pop();
      if (!spot) return null;
      claim(spot);
      guests.push({ id: id++, type, x: spot.x, y: spot.y, hp: guestDef(type).hp, aware: false, noticedOn: -1 });
    }
  }

  // Items can be anywhere in a room, including the start room.
  const itemSpots = rng.shuffle(rooms.flatMap(roomTiles).filter((t) => !taken.has(key(t.x, t.y))));
  const items = [];
  for (const type of [...ingredientsFor(floorIndex, rng), ...def.gear]) {
    const spot = itemSpots.pop();
    if (!spot) return null;
    claim(spot);
    items.push({ type, x: spot.x, y: spot.y });
  }

  return { map, rooms, player, guests, items, stairs, king };
}

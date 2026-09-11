import { TILE } from './constants.js';

const MIN_ROOMS = 4;

/**
 * Classic rooms-and-corridors generator. Rooms never overlap (1-tile padding)
 * and each room is joined to the previous one with an L-shaped corridor, so the
 * whole floor is always connected. Returns { w, h, tiles: string[], rooms }.
 */
export function generateDungeon(rng, w, h, opts = {}) {
  const { maxRooms = 9, minW = 4, maxW = 8, minH = 3, maxH = 5, attempts = 120 } = opts;

  for (let tries = 0; tries < 50; tries++) {
    const grid = Array.from({ length: h }, () => Array(w).fill(TILE.WALL));
    const rooms = [];

    for (let i = 0; i < attempts && rooms.length < maxRooms; i++) {
      const rw = rng.int(minW, maxW);
      const rh = rng.int(minH, maxH);
      const rx = rng.int(1, w - rw - 2);
      const ry = rng.int(1, h - rh - 2);
      const room = { x: rx, y: ry, w: rw, h: rh };
      if (rooms.some((r) => overlaps(r, room))) continue;

      carveRoom(grid, room);
      if (rooms.length > 0) carveCorridor(grid, center(rooms[rooms.length - 1]), center(room), rng);
      rooms.push(room);
    }

    if (rooms.length >= MIN_ROOMS) {
      return { w, h, tiles: grid.map((row) => row.join('')), rooms };
    }
  }
  throw new Error('Could not generate a dungeon');
}

export const center = (room) => ({ x: room.x + Math.floor(room.w / 2), y: room.y + Math.floor(room.h / 2) });

export function roomTiles(room) {
  const out = [];
  for (let y = room.y; y < room.y + room.h; y++) {
    for (let x = room.x; x < room.x + room.w; x++) out.push({ x, y });
  }
  return out;
}

function overlaps(a, b) {
  return a.x - 1 < b.x + b.w && a.x + a.w + 1 > b.x && a.y - 1 < b.y + b.h && a.y + a.h + 1 > b.y;
}

function carveRoom(grid, room) {
  for (const { x, y } of roomTiles(room)) grid[y][x] = TILE.FLOOR;
}

function carveCorridor(grid, from, to, rng) {
  const carveH = (y, x0, x1) => {
    for (let x = Math.min(x0, x1); x <= Math.max(x0, x1); x++) grid[y][x] = TILE.FLOOR;
  };
  const carveV = (x, y0, y1) => {
    for (let y = Math.min(y0, y1); y <= Math.max(y0, y1); y++) grid[y][x] = TILE.FLOOR;
  };
  if (rng.next() < 0.5) {
    carveH(from.y, from.x, to.x);
    carveV(to.x, from.y, to.y);
  } else {
    carveV(from.x, from.y, to.y);
    carveH(to.y, from.x, to.x);
  }
}

import { TILE, DIRS } from './constants.js';

export const key = (x, y) => `${x},${y}`;
export const idx = (map, x, y) => y * map.w + x;
export const inBounds = (map, x, y) => x >= 0 && x < map.w && y >= 0 && y < map.h;
export const tileAt = (map, x, y) => (inBounds(map, x, y) ? map.tiles[y][x] : TILE.WALL);
export const isFloor = (map, x, y) => tileAt(map, x, y) !== TILE.WALL;
export const chebyshev = (a, b) => Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y));

/** Bresenham line; true when every tile strictly between the endpoints is floor. */
export function lineOfSight(map, x0, y0, x1, y1) {
  const dx = Math.abs(x1 - x0);
  const dy = -Math.abs(y1 - y0);
  const sx = x0 < x1 ? 1 : -1;
  const sy = y0 < y1 ? 1 : -1;
  let err = dx + dy;
  let x = x0;
  let y = y0;
  for (;;) {
    if (x === x1 && y === y1) return true;
    if (!(x === x0 && y === y0) && !isFloor(map, x, y)) return false;
    const e2 = 2 * err;
    if (e2 >= dy) {
      err += dy;
      x += sx;
    }
    if (e2 <= dx) {
      err += dx;
      y += sy;
    }
  }
}

/** Set of "x,y" keys visible from origin within a square radius (walls included when lit). */
export function computeVisible(map, origin, radius) {
  const out = new Set();
  for (let y = origin.y - radius; y <= origin.y + radius; y++) {
    for (let x = origin.x - radius; x <= origin.x + radius; x++) {
      if (!inBounds(map, x, y)) continue;
      if (lineOfSight(map, origin.x, origin.y, x, y)) out.add(key(x, y));
    }
  }
  return out;
}

/**
 * Breadth-first path distances from origin over floor tiles (8-connected).
 * -1 means unreachable. `passable(x, y)` can further restrict the walk.
 */
export function distanceMap(map, origin, passable = () => true) {
  const dist = new Array(map.w * map.h).fill(-1);
  if (!isFloor(map, origin.x, origin.y)) return dist;
  const queue = [origin];
  dist[idx(map, origin.x, origin.y)] = 0;
  for (let head = 0; head < queue.length; head++) {
    const { x, y } = queue[head];
    const d = dist[idx(map, x, y)];
    for (const [dx, dy] of DIRS) {
      const nx = x + dx;
      const ny = y + dy;
      if (!isFloor(map, nx, ny) || !passable(nx, ny)) continue;
      const i = idx(map, nx, ny);
      if (dist[i] !== -1) continue;
      dist[i] = d + 1;
      queue.push({ x: nx, y: ny });
    }
  }
  return dist;
}

/** First step from `from` toward `to` along a shortest path, or null if unreachable. */
export function firstStep(map, from, to, passable = () => true) {
  const dist = distanceMap(map, to, passable);
  if (dist[idx(map, from.x, from.y)] <= 0) return null;
  let best = null;
  let bestD = Infinity;
  for (const [dx, dy] of DIRS) {
    const nx = from.x + dx;
    const ny = from.y + dy;
    if (!inBounds(map, nx, ny)) continue;
    const d = dist[idx(map, nx, ny)];
    if (d !== -1 && d < bestD) {
      bestD = d;
      best = { dx, dy };
    }
  }
  return best;
}

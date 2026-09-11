import { BOARD_SIZE } from './constants.js';

// Coordinates: x = file (0..7 left→right), y = rank (0 = bottom, 7 = top / promotion).
export const inBounds = (x, y) => x >= 0 && x < BOARD_SIZE && y >= 0 && y < BOARD_SIZE;
export const samePos = (a, b) => a.x === b.x && a.y === b.y;
export const key = (x, y) => `${x},${y}`;

export function enemyAt(state, x, y) {
  return state.enemies.find((e) => e.x === x && e.y === y) || null;
}

export function pickupAt(state, x, y) {
  return state.pickups.find((p) => p.x === x && p.y === y) || null;
}

export function isPlayerAt(state, x, y) {
  return state.player.x === x && state.player.y === y;
}

export function isOccupied(state, x, y) {
  return isPlayerAt(state, x, y) || !!enemyAt(state, x, y);
}

export const chebyshev = (a, b) => Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y));

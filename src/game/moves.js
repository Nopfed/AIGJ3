import { GUEST, DIRS } from './constants.js';
import { inBounds, isFloor, key } from './fov.js';

const KNIGHT_OFFSETS = [
  [1, 2], [2, 1], [2, -1], [1, -2],
  [-1, -2], [-2, -1], [-2, 1], [-1, 2],
];
const ORTHO = [[1, 0], [-1, 0], [0, 1], [0, -1]];
const DIAG = [[1, 1], [1, -1], [-1, 1], [-1, -1]];

export function guestAt(state, x, y) {
  return state.guests.find((g) => g.x === x && g.y === y) || null;
}
export function itemAt(state, x, y) {
  return state.items.find((i) => i.x === x && i.y === y) || null;
}
export const isPlayerAt = (state, x, y) => state.player.x === x && state.player.y === y;
export const isKingAt = (state, x, y) => !!state.king && state.king.x === x && state.king.y === y;

/** Something solid a guest can't share a tile with (walls are handled separately). */
function blocked(state, x, y) {
  return !!guestAt(state, x, y) || isKingAt(state, x, y);
}

/**
 * Where the chef can step: any of the 8 neighbours that is floor.
 * A guest or the King on the tile makes it a bump (attack / serve).
 */
export function playerMoves(state) {
  const { x, y } = state.player;
  const out = [];
  for (const [dx, dy] of DIRS) {
    const tx = x + dx;
    const ty = y + dy;
    if (!isFloor(state.map, tx, ty)) continue;
    const guest = guestAt(state, tx, ty);
    out.push({ x: tx, y: ty, dx, dy, guest, king: isKingAt(state, tx, ty) });
  }
  return out;
}

/**
 * Legal destinations for a guest using chess movement inside the dungeon.
 * Walls stop slides; other guests and the King block; the chef's tile is an
 * attack. Pages step orthogonally but only ever attack diagonally.
 * Returns [{x, y, attack}] — `attack` squares are never moved onto.
 */
export function guestMoves(state, guest) {
  return guestReach(state, guest.type, guest.x, guest.y);
}

function guestReach(state, type, x, y) {
  const { map } = state;
  const out = [];

  const step = (tx, ty, { canMove = true, canAttack = true } = {}) => {
    if (!isFloor(map, tx, ty)) return;
    if (isPlayerAt(state, tx, ty)) {
      if (canAttack) out.push({ x: tx, y: ty, attack: true });
      return;
    }
    if (canMove && !blocked(state, tx, ty)) out.push({ x: tx, y: ty, attack: false });
  };

  const slide = (dirs) => {
    for (const [dx, dy] of dirs) {
      let tx = x + dx;
      let ty = y + dy;
      while (inBounds(map, tx, ty) && isFloor(map, tx, ty)) {
        if (isPlayerAt(state, tx, ty)) {
          out.push({ x: tx, y: ty, attack: true });
          break;
        }
        if (blocked(state, tx, ty)) break;
        out.push({ x: tx, y: ty, attack: false });
        tx += dx;
        ty += dy;
      }
    }
  };

  switch (type) {
    case GUEST.PAGE:
      for (const [dx, dy] of ORTHO) step(x + dx, y + dy, { canAttack: false });
      for (const [dx, dy] of DIAG) step(x + dx, y + dy, { canMove: false });
      break;
    case GUEST.KNIGHT:
      for (const [dx, dy] of KNIGHT_OFFSETS) step(x + dx, y + dy);
      break;
    case GUEST.BISHOP:
      slide(DIAG);
      break;
    case GUEST.ROOK:
      slide(ORTHO);
      break;
    case GUEST.QUEEN:
      slide([...ORTHO, ...DIAG]);
      break;
    default:
      throw new Error(`Unknown guest type "${type}"`);
  }
  return out;
}

/**
 * Tiles a guest of `type` standing at (x, y) could attack next turn — the
 * squares the chef must not end a turn on. Occupancy is ignored except that
 * slides stop at the first solid thing they meet.
 */
export function guestThreats(state, type, x, y) {
  const { map } = state;
  const set = new Set();
  const add = (tx, ty) => {
    if (isFloor(map, tx, ty)) set.add(key(tx, ty));
  };
  const slide = (dirs) => {
    for (const [dx, dy] of dirs) {
      let tx = x + dx;
      let ty = y + dy;
      while (isFloor(map, tx, ty)) {
        set.add(key(tx, ty));
        if (blocked(state, tx, ty)) break;
        tx += dx;
        ty += dy;
      }
    }
  };
  switch (type) {
    case GUEST.PAGE:
      for (const [dx, dy] of DIAG) add(x + dx, y + dy);
      break;
    case GUEST.KNIGHT:
      for (const [dx, dy] of KNIGHT_OFFSETS) add(x + dx, y + dy);
      break;
    case GUEST.BISHOP:
      slide(DIAG);
      break;
    case GUEST.ROOK:
      slide(ORTHO);
      break;
    case GUEST.QUEEN:
      slide([...ORTHO, ...DIAG]);
      break;
    default:
      throw new Error(`Unknown guest type "${type}"`);
  }
  return set;
}

/** Union of threats from every guest that is currently aware of the chef. */
export function threatenedSquares(state, guests = state.guests.filter((g) => g.aware)) {
  const set = new Set();
  for (const g of guests) {
    for (const k of guestThreats(state, g.type, g.x, g.y)) set.add(k);
  }
  return set;
}

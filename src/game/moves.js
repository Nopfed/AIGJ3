import { PIECE, MODE } from './constants.js';
import { inBounds, enemyAt, isPlayerAt, isOccupied } from './board.js';

const KNIGHT_OFFSETS = [
  [1, 2], [2, 1], [2, -1], [1, -2],
  [-1, -2], [-2, -1], [-2, 1], [-1, 2],
];
const KING_OFFSETS = [
  [1, 0], [1, 1], [0, 1], [-1, 1],
  [-1, 0], [-1, -1], [0, -1], [1, -1],
];
const ROOK_DIRS = [[1, 0], [-1, 0], [0, 1], [0, -1]];
const BISHOP_DIRS = [[1, 1], [1, -1], [-1, 1], [-1, -1]];

/**
 * Player pawn moves: forward 1 (or 2 from the home rank), capture diagonally forward.
 * MODE.PEPPER: knight jump (may capture). MODE.CURRY: king step (may capture).
 * Returns [{x, y, capture: boolean}].
 */
export function playerMoves(state, mode = MODE.NORMAL) {
  const { x, y } = state.player;
  const out = [];

  if (mode === MODE.PEPPER) {
    for (const [dx, dy] of KNIGHT_OFFSETS) pushIfLegal(state, out, x + dx, y + dy);
    return out;
  }
  if (mode === MODE.CURRY) {
    for (const [dx, dy] of KING_OFFSETS) pushIfLegal(state, out, x + dx, y + dy);
    return out;
  }

  // Normal pawn movement.
  if (inBounds(x, y + 1) && !isOccupied(state, x, y + 1)) {
    out.push({ x, y: y + 1, capture: false });
    if (y === 0 && inBounds(x, y + 2) && !isOccupied(state, x, y + 2)) {
      out.push({ x, y: y + 2, capture: false });
    }
  }
  for (const dx of [-1, 1]) {
    const tx = x + dx;
    const ty = y + 1;
    if (inBounds(tx, ty) && enemyAt(state, tx, ty)) out.push({ x: tx, y: ty, capture: true });
  }
  return out;
}

function pushIfLegal(state, out, tx, ty) {
  if (!inBounds(tx, ty)) return;
  const enemy = enemyAt(state, tx, ty);
  out.push({ x: tx, y: ty, capture: !!enemy });
}

/**
 * Legal destinations for an enemy piece using real chess movement.
 * Enemies are blocked by other enemies; the player square is a capture.
 * Enemy pawns move DOWN the board (toward rank 0).
 */
export function enemyMoves(state, enemy) {
  const out = [];
  const { x, y, type } = enemy;

  const tryStep = (tx, ty) => {
    if (!inBounds(tx, ty)) return false;
    if (enemyAt(state, tx, ty)) return false;
    out.push({ x: tx, y: ty, capture: isPlayerAt(state, tx, ty) });
    return true;
  };

  const slide = (dirs) => {
    for (const [dx, dy] of dirs) {
      let tx = x + dx;
      let ty = y + dy;
      while (inBounds(tx, ty)) {
        if (enemyAt(state, tx, ty)) break;
        const capture = isPlayerAt(state, tx, ty);
        out.push({ x: tx, y: ty, capture });
        if (capture) break;
        tx += dx;
        ty += dy;
      }
    }
  };

  switch (type) {
    case PIECE.PAWN:
      if (inBounds(x, y - 1) && !isOccupied(state, x, y - 1)) out.push({ x, y: y - 1, capture: false });
      for (const dx of [-1, 1]) {
        if (isPlayerAt(state, x + dx, y - 1)) out.push({ x: x + dx, y: y - 1, capture: true });
      }
      break;
    case PIECE.KNIGHT:
      for (const [dx, dy] of KNIGHT_OFFSETS) tryStep(x + dx, y + dy);
      break;
    case PIECE.BISHOP:
      slide(BISHOP_DIRS);
      break;
    case PIECE.ROOK:
      slide(ROOK_DIRS);
      break;
    case PIECE.QUEEN:
      slide([...ROOK_DIRS, ...BISHOP_DIRS]);
      break;
    default:
      break;
  }
  return out;
}

/** Set of "x,y" keys currently attacked by any enemy (used for the threat overlay). */
export function threatenedSquares(state) {
  const set = new Set();
  for (const enemy of state.enemies) {
    if (enemy.type === PIECE.PAWN) {
      // Pawns only threaten diagonally, regardless of occupancy.
      for (const dx of [-1, 1]) {
        if (inBounds(enemy.x + dx, enemy.y - 1)) set.add(`${enemy.x + dx},${enemy.y - 1}`);
      }
      continue;
    }
    for (const m of enemyMoves(state, enemy)) set.add(`${m.x},${m.y}`);
  }
  return set;
}

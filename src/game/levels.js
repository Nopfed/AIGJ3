import { BOARD_SIZE, PIECE, PICKUP } from './constants.js';
import { createRng } from './rng.js';
import { threatenedSquares } from './moves.js';

// Enemy roster per level. Clear the last one to win the run.
export const LEVELS = [
  { name: 'The Pantry', enemies: { [PIECE.PAWN]: 2, [PIECE.KNIGHT]: 1 } },
  { name: 'The Prep Line', enemies: { [PIECE.PAWN]: 3, [PIECE.KNIGHT]: 1, [PIECE.BISHOP]: 1 } },
  { name: 'The Stove Top', enemies: { [PIECE.PAWN]: 2, [PIECE.KNIGHT]: 2, [PIECE.BISHOP]: 1, [PIECE.ROOK]: 1 } },
  { name: 'The Spice Rack', enemies: { [PIECE.PAWN]: 3, [PIECE.KNIGHT]: 1, [PIECE.BISHOP]: 2, [PIECE.ROOK]: 1 } },
  { name: "The Head Chef's Table", enemies: { [PIECE.PAWN]: 2, [PIECE.KNIGHT]: 2, [PIECE.BISHOP]: 1, [PIECE.ROOK]: 1, [PIECE.QUEEN]: 1 } },
];

const PICKUPS_PER_LEVEL = { [PICKUP.PEPPER]: 2, [PICKUP.CURRY]: 2 };
const ENEMY_MIN_RANK = 3; // enemies spawn on ranks 3..7 (y index)
const MAX_ATTEMPTS = 200;

/**
 * Build a level layout. Player starts on rank 0; the start square is guaranteed
 * not to be under attack. Returns { player, enemies, pickups }.
 */
export function generateLevel(levelIndex, seed) {
  const def = LEVELS[levelIndex];
  if (!def) throw new Error(`No level ${levelIndex}`);
  const rng = createRng(seed * 7919 + levelIndex * 104729);

  for (let attempt = 0; attempt < MAX_ATTEMPTS; attempt++) {
    const layout = tryLayout(def, rng);
    const threats = threatenedSquares(layout);
    if (!threats.has(`${layout.player.x},${layout.player.y}`)) return layout;
  }
  throw new Error('Could not generate a safe level layout');
}

function tryLayout(def, rng) {
  const player = { x: rng.int(0, BOARD_SIZE - 1), y: 0 };

  const enemySquares = [];
  for (let y = ENEMY_MIN_RANK; y < BOARD_SIZE; y++) {
    for (let x = 0; x < BOARD_SIZE; x++) enemySquares.push({ x, y });
  }
  const shuffled = rng.shuffle(enemySquares);

  const enemies = [];
  let id = 0;
  for (const [type, count] of Object.entries(def.enemies)) {
    for (let i = 0; i < count; i++) {
      const sq = shuffled.pop();
      enemies.push({ id: id++, type, x: sq.x, y: sq.y });
    }
  }

  // Pickups go on ranks 1..6, never on an enemy or the player.
  const taken = new Set(enemies.map((e) => `${e.x},${e.y}`));
  taken.add(`${player.x},${player.y}`);
  const pickupSquares = [];
  for (let y = 1; y < BOARD_SIZE - 1; y++) {
    for (let x = 0; x < BOARD_SIZE; x++) {
      if (!taken.has(`${x},${y}`)) pickupSquares.push({ x, y });
    }
  }
  const shuffledPickups = rng.shuffle(pickupSquares);
  const pickups = [];
  for (const [type, count] of Object.entries(PICKUPS_PER_LEVEL)) {
    for (let i = 0; i < count; i++) {
      const sq = shuffledPickups.pop();
      pickups.push({ type, x: sq.x, y: sq.y });
    }
  }

  return { player, enemies, pickups };
}

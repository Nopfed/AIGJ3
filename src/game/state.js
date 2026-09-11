import { BOARD_SIZE, MAX_HEARTS, MAX_CHARGES, MODE, PICKUP, STATUS } from './constants.js';
import { enemyAt, pickupAt, samePos } from './board.js';
import { playerMoves } from './moves.js';
import { chooseEnemyMove, enemyOrder } from './ai.js';
import { generateLevel, LEVELS } from './levels.js';

const clone = (state) => JSON.parse(JSON.stringify(state));

export function newGame(seed = Date.now() % 1_000_000) {
  const base = {
    seed,
    level: 0,
    hearts: MAX_HEARTS,
    charges: { [PICKUP.PEPPER]: 0, [PICKUP.CURRY]: 0 },
    mode: MODE.NORMAL,
    score: 0,
    turn: 0,
    status: STATUS.PLAYING,
    log: [],
  };
  return loadLevel(base, 0);
}

function loadLevel(state, levelIndex) {
  const layout = generateLevel(levelIndex, state.seed);
  return {
    ...state,
    level: levelIndex,
    player: layout.player,
    enemies: layout.enemies,
    pickups: layout.pickups,
    mode: MODE.NORMAL,
    turn: 0,
    status: STATUS.PLAYING,
    log: [`Level ${levelIndex + 1}: ${LEVELS[levelIndex].name}. Reach the top rank to promote.`],
  };
}

export function levelName(state) {
  return LEVELS[state.level].name;
}

export function isLastLevel(state) {
  return state.level >= LEVELS.length - 1;
}

/** Toggle a power mode for the next move. Requires a charge of that type. */
export function setMode(state, mode) {
  if (state.status !== STATUS.PLAYING) return state;
  const next = clone(state);
  if (mode === MODE.NORMAL || next.mode === mode) {
    next.mode = MODE.NORMAL;
    return next;
  }
  const chargeType = mode === MODE.PEPPER ? PICKUP.PEPPER : PICKUP.CURRY;
  if (next.charges[chargeType] <= 0) return state;
  next.mode = mode;
  return next;
}

export function legalMoves(state) {
  if (state.status !== STATUS.PLAYING) return [];
  return playerMoves(state, state.mode);
}

/** Apply a player move (must be one of legalMoves), then run the enemy turn. */
export function movePlayer(state, target) {
  if (state.status !== STATUS.PLAYING) return state;
  const move = legalMoves(state).find((m) => samePos(m, target));
  if (!move) return state;

  const next = clone(state);
  next.turn += 1;
  next.log = [];

  // Spend the charge behind a powered move.
  if (next.mode === MODE.PEPPER) next.charges[PICKUP.PEPPER] -= 1;
  if (next.mode === MODE.CURRY) next.charges[PICKUP.CURRY] -= 1;
  next.mode = MODE.NORMAL;

  if (move.capture) {
    const victim = enemyAt(next, move.x, move.y);
    next.enemies = next.enemies.filter((e) => e.id !== victim.id);
    next.score += 10;
    next.log.push(`Captured a ${victim.type}! +10`);
  }

  next.player = { x: move.x, y: move.y };

  const pickup = pickupAt(next, move.x, move.y);
  if (pickup) {
    next.pickups = next.pickups.filter((p) => !samePos(p, pickup));
    if (next.charges[pickup.type] < MAX_CHARGES) {
      next.charges[pickup.type] += 1;
      next.log.push(pickup.type === PICKUP.PEPPER ? 'Picked up a pepper.' : 'Picked up curry.');
    } else {
      next.score += 5;
      next.log.push(`Pouch full, ate the ${pickup.type} instead. +5`);
    }
  }

  // Promotion!
  if (next.player.y === BOARD_SIZE - 1) {
    next.score += 50;
    if (isLastLevel(next)) {
      next.status = STATUS.WON;
      next.log.push('Promoted to Queen. You are the Head Chef now! +50');
    } else {
      next.status = STATUS.LEVEL_CLEAR;
      next.hearts = Math.min(MAX_HEARTS, next.hearts + 1);
      next.log.push('Promoted! +50 and a heart restored.');
    }
    return next;
  }

  return runEnemyTurn(next);
}

function runEnemyTurn(next) {
  for (const ordered of enemyOrder(next)) {
    const enemy = next.enemies.find((e) => e.id === ordered.id);
    if (!enemy) continue;
    const move = chooseEnemyMove(next, enemy);
    if (!move) continue;

    enemy.x = move.x;
    enemy.y = move.y;

    if (move.capture) {
      next.hearts -= 1;
      next.log.push(`A ${enemy.type} caught you! Knocked back to your home rank.`);
      if (next.hearts <= 0) {
        next.status = STATUS.LOST;
        next.log.push('Out of hearts. The kitchen wins this time.');
        return next;
      }
      // Knockback ends the enemy turn so a single misstep never costs two hearts.
      next.player = findHomeSquare(next, next.player.x);
      return next;
    }
  }
  return next;
}

/** Nearest free square on rank 0 to the given file. */
function findHomeSquare(state, preferredX) {
  for (let d = 0; d < BOARD_SIZE; d++) {
    for (const x of [preferredX - d, preferredX + d]) {
      if (x < 0 || x >= BOARD_SIZE) continue;
      if (!enemyAt(state, x, 0)) return { x, y: 0 };
    }
  }
  return { x: preferredX, y: 0 };
}

export function nextLevel(state) {
  if (state.status !== STATUS.LEVEL_CLEAR) return state;
  return loadLevel(clone(state), state.level + 1);
}

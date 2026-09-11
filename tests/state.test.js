import { describe, it, expect } from 'vitest';
import { newGame, movePlayer, setMode, nextLevel, legalMoves } from '../src/game/state.js';
import { chooseEnemyMove } from '../src/game/ai.js';
import { generateLevel, LEVELS } from '../src/game/levels.js';
import { threatenedSquares } from '../src/game/moves.js';
import { MODE, PIECE, PICKUP, STATUS, MAX_HEARTS, MAX_CHARGES } from '../src/game/constants.js';

/** Hand-built state so tests don't depend on level generation. */
function scenario(overrides = {}) {
  return {
    seed: 1,
    level: 0,
    hearts: MAX_HEARTS,
    charges: { [PICKUP.PEPPER]: 0, [PICKUP.CURRY]: 0 },
    mode: MODE.NORMAL,
    score: 0,
    turn: 0,
    status: STATUS.PLAYING,
    log: [],
    player: { x: 0, y: 0 },
    enemies: [],
    pickups: [],
    ...overrides,
  };
}

describe('level generation', () => {
  it('is deterministic for a seed and never starts the player under attack', () => {
    for (let level = 0; level < LEVELS.length; level++) {
      for (let seed = 1; seed <= 25; seed++) {
        const a = generateLevel(level, seed);
        const b = generateLevel(level, seed);
        expect(a).toEqual(b);
        expect(a.player.y).toBe(0);
        expect(threatenedSquares(a).has(`${a.player.x},${a.player.y}`)).toBe(false);

        const expected = Object.values(LEVELS[level].enemies).reduce((s, n) => s + n, 0);
        expect(a.enemies).toHaveLength(expected);
        expect(a.pickups).toHaveLength(4);

        const occupied = new Set([...a.enemies, ...a.pickups].map((p) => `${p.x},${p.y}`));
        expect(occupied.size).toBe(a.enemies.length + a.pickups.length); // no overlaps
      }
    }
  });
});

describe('newGame', () => {
  it('starts on level 1 with full hearts and no charges', () => {
    const s = newGame(42);
    expect(s.level).toBe(0);
    expect(s.hearts).toBe(MAX_HEARTS);
    expect(s.charges).toEqual({ pepper: 0, curry: 0 });
    expect(s.status).toBe(STATUS.PLAYING);
    expect(legalMoves(s).length).toBeGreaterThan(0);
  });
});

describe('movePlayer', () => {
  it('ignores illegal targets', () => {
    const s = scenario();
    expect(movePlayer(s, { x: 5, y: 5 })).toBe(s);
  });

  it('moves the pawn and advances the turn', () => {
    const s = movePlayer(scenario(), { x: 0, y: 2 });
    expect(s.player).toEqual({ x: 0, y: 2 });
    expect(s.turn).toBe(1);
  });

  it('captures an enemy diagonally and scores', () => {
    const s = scenario({ enemies: [{ id: 0, type: PIECE.KNIGHT, x: 1, y: 1 }] });
    const next = movePlayer(s, { x: 1, y: 1 });
    expect(next.enemies).toHaveLength(0);
    expect(next.score).toBe(10);
  });

  it('collects pickups into charges, capped at MAX_CHARGES', () => {
    const s = scenario({
      charges: { pepper: MAX_CHARGES, curry: 0 },
      pickups: [
        { type: PICKUP.PEPPER, x: 0, y: 1 },
        { type: PICKUP.CURRY, x: 0, y: 2 },
      ],
    });
    const a = movePlayer(s, { x: 0, y: 1 });
    expect(a.charges.pepper).toBe(MAX_CHARGES);
    expect(a.score).toBe(5); // overflow eaten for points
    expect(a.pickups).toHaveLength(1);
    const b = movePlayer(a, { x: 0, y: 2 });
    expect(b.charges.curry).toBe(1);
    expect(b.pickups).toHaveLength(0);
  });

  it('promotes on the top rank, restores a heart and flags LEVEL_CLEAR', () => {
    const s = scenario({ player: { x: 4, y: 6 }, hearts: 1 });
    const next = movePlayer(s, { x: 4, y: 7 });
    expect(next.status).toBe(STATUS.LEVEL_CLEAR);
    expect(next.hearts).toBe(2);
    expect(next.score).toBe(50);
  });

  it('wins the run when promoting on the last level', () => {
    const s = scenario({ level: LEVELS.length - 1, player: { x: 4, y: 6 } });
    expect(movePlayer(s, { x: 4, y: 7 }).status).toBe(STATUS.WON);
  });
});

describe('power modes', () => {
  it('cannot activate a mode without a charge', () => {
    const s = scenario();
    expect(setMode(s, MODE.PEPPER)).toBe(s);
  });

  it('toggles a mode on/off and spends the charge on the move', () => {
    const s = scenario({ charges: { pepper: 1, curry: 0 } });
    const armed = setMode(s, MODE.PEPPER);
    expect(armed.mode).toBe(MODE.PEPPER);
    expect(setMode(armed, MODE.PEPPER).mode).toBe(MODE.NORMAL);

    const moved = movePlayer(armed, { x: 1, y: 2 });
    expect(moved.player).toEqual({ x: 1, y: 2 });
    expect(moved.charges.pepper).toBe(0);
    expect(moved.mode).toBe(MODE.NORMAL);
  });

  it('curry step can move backwards', () => {
    const s = setMode(scenario({ player: { x: 3, y: 3 }, charges: { pepper: 0, curry: 1 } }), MODE.CURRY);
    expect(movePlayer(s, { x: 3, y: 2 }).player).toEqual({ x: 3, y: 2 });
  });
});

describe('enemy turn', () => {
  it('an enemy that can capture does so; player loses a heart and is knocked home', () => {
    // Rook on the same file, clear line: after the pawn steps to (0,1) the rook takes it.
    const s = scenario({ enemies: [{ id: 0, type: PIECE.ROOK, x: 0, y: 7 }] });
    const next = movePlayer(s, { x: 0, y: 1 });
    expect(next.hearts).toBe(MAX_HEARTS - 1);
    expect(next.enemies[0]).toMatchObject({ x: 0, y: 1 });
    expect(next.player.y).toBe(0);
    expect(next.status).toBe(STATUS.PLAYING);
  });

  it('game is lost when hearts hit zero', () => {
    const s = scenario({ hearts: 1, enemies: [{ id: 0, type: PIECE.ROOK, x: 0, y: 7 }] });
    expect(movePlayer(s, { x: 0, y: 1 }).status).toBe(STATUS.LOST);
  });

  it('enemies approach the player when they cannot capture', () => {
    const s = scenario({ player: { x: 0, y: 0 }, enemies: [{ id: 0, type: PIECE.KNIGHT, x: 7, y: 7 }] });
    const move = chooseEnemyMove(s, s.enemies[0]);
    expect(move).not.toBeNull();
    expect(Math.max(Math.abs(move.x - 0), Math.abs(move.y - 0))).toBeLessThan(7);
  });

  it('enemies stay put rather than move away', () => {
    // Enemy pawn directly below the player cannot move closer (only moves down).
    const s = scenario({ player: { x: 3, y: 5 }, enemies: [{ id: 0, type: PIECE.PAWN, x: 3, y: 4 }] });
    expect(chooseEnemyMove(s, s.enemies[0])).toBeNull();
  });
});

describe('nextLevel', () => {
  it('only advances from LEVEL_CLEAR and keeps score/hearts', () => {
    const playing = scenario();
    expect(nextLevel(playing)).toBe(playing);

    const cleared = scenario({ status: STATUS.LEVEL_CLEAR, score: 120, hearts: 2, seed: 7 });
    const next = nextLevel(cleared);
    expect(next.level).toBe(1);
    expect(next.score).toBe(120);
    expect(next.hearts).toBe(2);
    expect(next.status).toBe(STATUS.PLAYING);
    expect(next.enemies.length).toBeGreaterThan(cleared.enemies.length);
  });
});

describe('knockback', () => {
  it('ends the enemy turn so only one heart is lost per player move', () => {
    const s = scenario({
      player: { x: 3, y: 0 },
      enemies: [
        { id: 0, type: PIECE.ROOK, x: 3, y: 7 }, // captures at (3,1)
        { id: 1, type: PIECE.QUEEN, x: 7, y: 7 }, // same distance, acts after the rook
      ],
    });
    const next = movePlayer(s, { x: 3, y: 1 });
    expect(next.hearts).toBe(MAX_HEARTS - 1);
    expect(next.enemies.find((e) => e.id === 1)).toMatchObject({ x: 7, y: 7 }); // did not act
  });
});

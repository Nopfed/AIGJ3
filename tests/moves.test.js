import { describe, it, expect } from 'vitest';
import { playerMoves, enemyMoves, threatenedSquares } from '../src/game/moves.js';
import { MODE, PIECE } from '../src/game/constants.js';

const mk = (player, enemies = [], pickups = []) => ({
  player,
  enemies: enemies.map((e, i) => ({ id: i, ...e })),
  pickups,
});
const sorted = (moves) => moves.map((m) => `${m.x},${m.y}${m.capture ? '!' : ''}`).sort();

describe('playerMoves (normal pawn)', () => {
  it('moves forward 1 or 2 from the home rank', () => {
    expect(sorted(playerMoves(mk({ x: 3, y: 0 })))).toEqual(['3,1', '3,2']);
  });

  it('moves forward only 1 once off the home rank', () => {
    expect(sorted(playerMoves(mk({ x: 3, y: 2 })))).toEqual(['3,3']);
  });

  it('is blocked by a piece directly ahead and cannot double-jump over one', () => {
    const s = mk({ x: 3, y: 0 }, [{ type: PIECE.PAWN, x: 3, y: 1 }]);
    expect(playerMoves(s)).toEqual([]);
    const s2 = mk({ x: 3, y: 0 }, [{ type: PIECE.PAWN, x: 3, y: 2 }]);
    expect(sorted(playerMoves(s2))).toEqual(['3,1']);
  });

  it('captures diagonally forward only', () => {
    const s = mk({ x: 3, y: 2 }, [
      { type: PIECE.PAWN, x: 2, y: 3 },
      { type: PIECE.PAWN, x: 4, y: 3 },
      { type: PIECE.PAWN, x: 2, y: 1 }, // behind — not capturable
    ]);
    expect(sorted(playerMoves(s))).toEqual(['2,3!', '3,3', '4,3!']);
  });

  it('has no moves on the top rank', () => {
    expect(playerMoves(mk({ x: 0, y: 7 }))).toEqual([]);
  });
});

describe('playerMoves (powered)', () => {
  it('pepper mode gives knight jumps that can capture', () => {
    const s = mk({ x: 0, y: 0 }, [{ type: PIECE.ROOK, x: 1, y: 2 }]);
    expect(sorted(playerMoves(s, MODE.PEPPER))).toEqual(['1,2!', '2,1']);
  });

  it('curry mode gives king steps in all directions, including backwards', () => {
    const s = mk({ x: 4, y: 4 }, [{ type: PIECE.BISHOP, x: 4, y: 3 }]);
    const moves = playerMoves(s, MODE.CURRY);
    expect(moves).toHaveLength(8);
    expect(moves.find((m) => m.x === 4 && m.y === 3).capture).toBe(true);
  });
});

describe('enemyMoves', () => {
  it('enemy pawns move down and capture diagonally down', () => {
    const s = mk({ x: 2, y: 4 }, [{ type: PIECE.PAWN, x: 3, y: 5 }]);
    expect(sorted(enemyMoves(s, s.enemies[0]))).toEqual(['2,4!', '3,4']);
  });

  it('rooks slide until blocked by a friendly piece and stop on the player', () => {
    const s = mk({ x: 0, y: 3 }, [
      { type: PIECE.ROOK, x: 0, y: 7 },
      { type: PIECE.PAWN, x: 2, y: 7 },
    ]);
    const moves = sorted(enemyMoves(s, s.enemies[0]));
    expect(moves).toEqual(['0,3!', '0,4', '0,5', '0,6', '1,7']);
  });

  it('knights jump over pieces and cannot land on friendlies', () => {
    const s = mk({ x: 7, y: 7 }, [
      { type: PIECE.KNIGHT, x: 0, y: 0 },
      { type: PIECE.PAWN, x: 1, y: 2 },
    ]);
    expect(sorted(enemyMoves(s, s.enemies[0]))).toEqual(['2,1']);
  });

  it('queens combine rook and bishop lines', () => {
    const s = mk({ x: 7, y: 7 }, [{ type: PIECE.QUEEN, x: 0, y: 0 }]);
    const moves = enemyMoves(s, s.enemies[0]);
    expect(moves).toHaveLength(21); // 7 up + 7 right + 7 diagonal
    expect(moves.filter((m) => m.capture)).toHaveLength(1);
  });
});

describe('threatenedSquares', () => {
  it('marks pawn diagonals even when empty, and never the square ahead', () => {
    const s = mk({ x: 7, y: 0 }, [{ type: PIECE.PAWN, x: 3, y: 5 }]);
    const t = threatenedSquares(s);
    expect(t.has('2,4')).toBe(true);
    expect(t.has('4,4')).toBe(true);
    expect(t.has('3,4')).toBe(false);
  });

  it('marks slider lines up to the first blocker', () => {
    const s = mk({ x: 7, y: 0 }, [
      { type: PIECE.BISHOP, x: 0, y: 0 },
      { type: PIECE.PAWN, x: 3, y: 3 },
    ]);
    const t = threatenedSquares(s);
    expect(t.has('1,1')).toBe(true);
    expect(t.has('2,2')).toBe(true);
    expect(t.has('3,3')).toBe(false);
    expect(t.has('4,4')).toBe(false);
  });
});

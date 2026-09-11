import { describe, it, expect } from 'vitest';
import { playerMoves, guestMoves, guestThreats, threatenedSquares } from '../src/game/moves.js';
import { chooseGuestAction } from '../src/game/ai.js';
import { distanceMap, lineOfSight, computeVisible, firstStep, idx } from '../src/game/fov.js';
import { GUEST } from '../src/game/constants.js';

/** Build a tiny map from ASCII rows. Coordinates: x = column, y = row. */
function mapOf(rows) {
  return { w: rows[0].length, h: rows.length, tiles: rows };
}

const mk = (rows, player, guests = [], extra = {}) => ({
  map: mapOf(rows),
  player,
  guests: guests.map((g, i) => ({ id: i, aware: true, noticedOn: -1, hp: 3, ...g })),
  items: [],
  king: null,
  ...extra,
});

const sorted = (moves) => moves.map((m) => `${m.x},${m.y}${m.attack ? '!' : ''}`).sort();

const OPEN = [
  '#######',
  '#.....#',
  '#.....#',
  '#.....#',
  '#.....#',
  '#.....#',
  '#######',
];

describe('playerMoves', () => {
  it('steps in all eight directions but never into walls', () => {
    const s = mk(OPEN, { x: 1, y: 1 });
    expect(playerMoves(s).map((m) => `${m.x},${m.y}`).sort()).toEqual(['1,2', '2,1', '2,2']);
  });

  it('marks adjacent guests and the King as bumps', () => {
    const s = mk(OPEN, { x: 3, y: 3 }, [{ type: GUEST.PAGE, x: 4, y: 3 }], { king: { x: 2, y: 2 } });
    const moves = playerMoves(s);
    expect(moves.find((m) => m.x === 4 && m.y === 3).guest.type).toBe(GUEST.PAGE);
    expect(moves.find((m) => m.x === 2 && m.y === 2).king).toBe(true);
    expect(moves).toHaveLength(8);
  });
});

describe('guestMoves in a dungeon', () => {
  it('pages step orthogonally and attack only diagonally', () => {
    const s = mk(OPEN, { x: 4, y: 4 }, [{ type: GUEST.PAGE, x: 3, y: 3 }]);
    expect(sorted(guestMoves(s, s.guests[0]))).toEqual(['2,3', '3,2', '3,4', '4,3', '4,4!']);
    // Orthogonally adjacent chef is NOT attackable by a page.
    const s2 = mk(OPEN, { x: 3, y: 4 }, [{ type: GUEST.PAGE, x: 3, y: 3 }]);
    expect(guestMoves(s2, s2.guests[0]).some((m) => m.attack)).toBe(false);
  });

  it('rooks slide until a wall and stop at other guests', () => {
    const s = mk(OPEN, { x: 5, y: 5 }, [
      { type: GUEST.ROOK, x: 1, y: 3 },
      { type: GUEST.PAGE, x: 4, y: 3 },
    ]);
    expect(sorted(guestMoves(s, s.guests[0]))).toEqual(['1,1', '1,2', '1,4', '1,5', '2,3', '3,3']);
  });

  it('bishops attack the chef along an open diagonal', () => {
    const s = mk(OPEN, { x: 4, y: 4 }, [{ type: GUEST.BISHOP, x: 1, y: 1 }]);
    expect(guestMoves(s, s.guests[0])).toContainEqual({ x: 4, y: 4, attack: true });
    expect(guestMoves(s, s.guests[0])).not.toContainEqual({ x: 5, y: 5, attack: false });
  });

  it('knights jump over walls but need floor to land on', () => {
    const rows = [
      '#######',
      '#.#...#',
      '#.#...#',
      '#.#...#',
      '#######',
    ];
    const s = mk(rows, { x: 3, y: 2 }, [{ type: GUEST.KNIGHT, x: 1, y: 1 }]);
    // (1,1) -> (3,2) is an L over the wall column; (2,3) and (3,0) are walls.
    expect(sorted(guestMoves(s, s.guests[0]))).toEqual(['3,2!']);
  });

  it('queens combine rook and bishop lines', () => {
    const s = mk(OPEN, { x: 5, y: 5 }, [{ type: GUEST.QUEEN, x: 3, y: 3 }]);
    const moves = guestMoves(s, s.guests[0]);
    expect(moves).toContainEqual({ x: 3, y: 1, attack: false });
    expect(moves).toContainEqual({ x: 1, y: 1, attack: false });
    expect(moves).toContainEqual({ x: 5, y: 5, attack: true });
  });

  it('rejects unknown guest types', () => {
    const s = mk(OPEN, { x: 5, y: 5 }, [{ type: 'dragon', x: 3, y: 3 }]);
    expect(() => guestMoves(s, s.guests[0])).toThrow();
  });
});

describe('threats', () => {
  it('shows page diagonals and knight L-squares regardless of occupancy', () => {
    const s = mk(OPEN, { x: 5, y: 5 }, [
      { type: GUEST.PAGE, x: 1, y: 1 },
      { type: GUEST.KNIGHT, x: 3, y: 3 },
    ]);
    expect([...guestThreats(s, GUEST.PAGE, 1, 1)].sort()).toEqual(['2,2']);
    expect(guestThreats(s, GUEST.KNIGHT, 3, 3).size).toBe(8);
  });

  it('only counts aware guests by default', () => {
    const s = mk(OPEN, { x: 5, y: 5 }, [{ type: GUEST.ROOK, x: 1, y: 1, aware: false }]);
    expect(threatenedSquares(s).size).toBe(0);
    expect(threatenedSquares(s, s.guests).size).toBe(8);
  });
});

describe('chooseGuestAction', () => {
  it('attacks when the chef is in reach', () => {
    const s = mk(OPEN, { x: 4, y: 1 }, [{ type: GUEST.ROOK, x: 1, y: 1 }]);
    expect(chooseGuestAction(s, s.guests[0], distanceMap(s.map, s.player))).toEqual({ kind: 'attack' });
  });

  it('lines up a shot when it can', () => {
    const s = mk(OPEN, { x: 4, y: 2 }, [{ type: GUEST.ROOK, x: 1, y: 1 }]);
    const action = chooseGuestAction(s, s.guests[0], distanceMap(s.map, s.player));
    expect(action.kind).toBe('move');
    // Either (1,2) or (4,1) puts the chef on a rook line; nearest by path wins.
    expect(['4,1', '1,2']).toContain(`${action.x},${action.y}`);
    expect(guestThreats(s, GUEST.ROOK, action.x, action.y).has('4,2')).toBe(true);
  });

  it('otherwise steps closer, and holds rather than drift away', () => {
    const s = mk(OPEN, { x: 5, y: 5 }, [{ type: GUEST.PAGE, x: 1, y: 1 }]);
    const action = chooseGuestAction(s, s.guests[0], distanceMap(s.map, s.player));
    expect(action.kind).toBe('move');
    expect(['2,1', '1,2']).toContain(`${action.x},${action.y}`);

    // A page orthogonally adjacent can't attack and can't get closer: hold.
    const s2 = mk(OPEN, { x: 3, y: 3 }, [{ type: GUEST.PAGE, x: 3, y: 2 }]);
    const a2 = chooseGuestAction(s2, s2.guests[0], distanceMap(s2.map, s2.player));
    expect(a2 === null || guestThreats(s2, GUEST.PAGE, a2.x, a2.y).has('3,3')).toBe(true);
  });
});

describe('fov and pathing', () => {
  const rows = [
    '#########',
    '#...#...#',
    '#...#...#',
    '#.......#',
    '#########',
  ];
  const map = mapOf(rows);

  it('line of sight is blocked by walls', () => {
    expect(lineOfSight(map, 1, 1, 3, 1)).toBe(true);
    expect(lineOfSight(map, 1, 1, 7, 1)).toBe(false);
    expect(lineOfSight(map, 1, 3, 7, 3)).toBe(true);
  });

  it('visible set includes the lit walls and excludes the far room', () => {
    const vis = computeVisible(map, { x: 2, y: 2 }, 7);
    expect(vis.has('4,2')).toBe(true); // the wall you are looking at
    expect(vis.has('6,1')).toBe(false);
    expect(vis.has('2,2')).toBe(true);
  });

  it('distance map walks around walls and marks unreachable tiles', () => {
    const dist = distanceMap(map, { x: 1, y: 1 });
    expect(dist[idx(map, 7, 1)]).toBe(6);
    expect(dist[idx(map, 4, 1)]).toBe(-1);
    const step = firstStep(map, { x: 1, y: 1 }, { x: 7, y: 1 });
    expect(step.dx).toBe(1);
    expect([0, 1]).toContain(step.dy);
    expect(firstStep(map, { x: 1, y: 1 }, { x: 1, y: 1 })).toBeNull();
  });
});

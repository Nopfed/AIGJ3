import { chebyshev, idx, key } from './fov.js';
import { guestMoves, guestThreats } from './moves.js';

/**
 * Decide one guest's action. `dist` is the BFS distance map from the chef.
 *  1. Attack if the chef is in reach.
 *  2. Otherwise move to a square from which the chef would be in reach
 *     (the closest such square), so sliders line up their shots.
 *  3. Otherwise step to the legal square nearest the chef by path distance.
 *  4. Hold still rather than drift away.
 * Deterministic, so the AI is readable and testable.
 * Returns { kind: 'attack' } | { kind: 'move', x, y } | null.
 */
export function chooseGuestAction(state, guest, dist) {
  const moves = guestMoves(state, guest);
  if (moves.some((m) => m.attack)) return { kind: 'attack' };

  const playerKey = key(state.player.x, state.player.y);
  // Path distance first; Manhattan distance breaks ties so a piece that is
  // diagonally aligned still shuffles closer instead of freezing.
  const score = (p) => {
    const d = dist[idx(state.map, p.x, p.y)];
    if (d === -1) return Infinity;
    return d * 100 + Math.abs(p.x - state.player.x) + Math.abs(p.y - state.player.y);
  };
  const here = score(guest);

  let setup = null;
  let setupScore = Infinity;
  let nearest = null;
  let nearestScore = Infinity;
  for (const m of moves) {
    if (m.attack) continue;
    const s = score(m);
    if (s === Infinity) continue;
    if (s < setupScore && guestThreats(state, guest.type, m.x, m.y).has(playerKey)) {
      setupScore = s;
      setup = m;
    }
    if (s < nearestScore) {
      nearestScore = s;
      nearest = m;
    }
  }

  if (setup) return { kind: 'move', x: setup.x, y: setup.y };
  if (nearest && nearestScore < here) return { kind: 'move', x: nearest.x, y: nearest.y };
  return null;
}

/** Guests act closest-first so the pressure feels fair and readable. */
export function guestOrder(state) {
  return state.guests
    .slice()
    .sort((a, b) => chebyshev(a, state.player) - chebyshev(b, state.player) || a.id - b.id);
}

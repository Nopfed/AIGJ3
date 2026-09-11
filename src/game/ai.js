import { chebyshev } from './board.js';
import { enemyMoves } from './moves.js';

/**
 * Decide one enemy's move. Captures the player if it can; otherwise steps to the
 * legal square closest to the player. Deterministic (first best in move order)
 * so the AI is predictable and testable. Returns a move or null (no legal move).
 */
export function chooseEnemyMove(state, enemy) {
  const moves = enemyMoves(state, enemy);
  if (moves.length === 0) return null;

  const capture = moves.find((m) => m.capture);
  if (capture) return capture;

  let best = null;
  let bestDist = Infinity;
  for (const m of moves) {
    const d = chebyshev(m, state.player);
    if (d < bestDist) {
      bestDist = d;
      best = m;
    }
  }
  // Don't drift away: staying put beats moving further from the player.
  if (bestDist > chebyshev(enemy, state.player)) return null;
  return best;
}

/** Enemies act closest-first so the pressure feels fair and readable. */
export function enemyOrder(state) {
  return state.enemies
    .slice()
    .sort((a, b) => chebyshev(a, state.player) - chebyshev(b, state.player) || a.id - b.id);
}

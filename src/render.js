import { BOARD_SIZE, MAX_HEARTS, MODE, PICKUP, STATUS } from './game/constants.js';
import { enemyAt, pickupAt, isPlayerAt } from './game/board.js';
import { threatenedSquares } from './game/moves.js';
import { legalMoves, levelName } from './game/state.js';
import { LEVELS } from './game/levels.js';
import { playerSprite, enemySprite, pickupSprite, pickupIcon, heartIcon } from './art/sprites.js';

const $ = (id) => document.getElementById(id);

/** Build the 64 square elements once; later renders only update classes/content. */
export function createBoard(boardEl, onSquareClick) {
  const squares = [];
  // Render top rank first so y=7 is at the top of the screen.
  for (let y = BOARD_SIZE - 1; y >= 0; y--) {
    for (let x = 0; x < BOARD_SIZE; x++) {
      const el = document.createElement('div');
      el.className = `sq ${(x + y) % 2 === 0 ? 'dark' : 'light'}`;
      el.dataset.x = x;
      el.dataset.y = y;
      el.addEventListener('click', () => onSquareClick(x, y));
      boardEl.appendChild(el);
      squares.push(el);
    }
  }
  return squares;
}

/** Static HUD bits that never change between renders. */
export function decorateHud() {
  $('pepper-icon').innerHTML = pickupIcon(PICKUP.PEPPER);
  $('curry-icon').innerHTML = pickupIcon(PICKUP.CURRY);
}

export function render(state, squares) {
  const threats = threatenedSquares(state);
  const moves = new Map(legalMoves(state).map((m) => [`${m.x},${m.y}`, m]));

  for (const el of squares) {
    const x = Number(el.dataset.x);
    const y = Number(el.dataset.y);
    const move = moves.get(`${x},${y}`);
    el.classList.toggle('move', !!move);

    // Paper layers, back to front: attacked tissue, promotion strip, piece, marker.
    let html = '';
    if (threats.has(`${x},${y}`)) html += '<span class="layer threat"></span>';
    if (y === BOARD_SIZE - 1) html += '<span class="layer goal"></span>';

    if (isPlayerAt(state, x, y)) {
      html += playerSprite(x, y);
    } else {
      const enemy = enemyAt(state, x, y);
      const pickup = pickupAt(state, x, y);
      if (enemy) html += enemySprite(enemy.type, x, y);
      else if (pickup) html += pickupSprite(pickup.type, x, y);
    }

    if (move) html += move.capture ? '<span class="layer capture"></span>' : '<span class="layer dot"></span>';
    el.innerHTML = html;
  }

  $('level').textContent = `${state.level + 1}/${LEVELS.length} · ${levelName(state)}`;
  $('hearts').innerHTML = Array.from({ length: MAX_HEARTS }, (_, i) => heartIcon(i < state.hearts)).join('');
  $('score').textContent = state.score;
  $('turn').textContent = state.turn;

  const pepperBtn = $('btn-pepper');
  const curryBtn = $('btn-curry');
  pepperBtn.disabled = state.charges[PICKUP.PEPPER] === 0 || state.status !== STATUS.PLAYING;
  curryBtn.disabled = state.charges[PICKUP.CURRY] === 0 || state.status !== STATUS.PLAYING;
  pepperBtn.classList.toggle('active', state.mode === MODE.PEPPER);
  curryBtn.classList.toggle('active', state.mode === MODE.CURRY);
  $('pepper-count').textContent = `×${state.charges[PICKUP.PEPPER]}`;
  $('curry-count').textContent = `×${state.charges[PICKUP.CURRY]}`;

  const cls = state.status === STATUS.WON ? 'win' : state.status === STATUS.LOST ? 'lose' : '';
  $('log').innerHTML = state.log.map((line) => `<p class="${cls}">${line}</p>`).join('');

  $('btn-next').hidden = state.status !== STATUS.LEVEL_CLEAR;
}

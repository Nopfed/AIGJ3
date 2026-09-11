import { STATUS } from './game/constants.js';
import { newGame, stepPlayer, waitTurn, selectSlot, stepToward } from './game/state.js';
import { createBoard, decorateHud, render } from './render.js';

const BEST_KEY = 'kings-chef-best-tips';
const ui = { best: readBest() };

// `?seed=123` replays a specific castle layout (handy for sharing a run).
const seedParam = Number(new URLSearchParams(location.search).get('seed'));
let state = newGame(Number.isInteger(seedParam) && seedParam > 0 ? seedParam : undefined);

const boardEl = document.getElementById('board');
const tiles = createBoard(boardEl, (x, y) => update(stepToward(state, x, y)));

function update(next) {
  const hurt = next.patience < state.patience;
  state = next;
  if (state.status !== STATUS.PLAYING && state.tips > ui.best) {
    ui.best = state.tips;
    writeBest(ui.best);
  }
  render(state, tiles, ui);
  if (hurt) flash(boardEl, 'hurt');
}

/** Re-trigger a one-shot CSS animation class. */
function flash(el, cls) {
  el.classList.remove(cls);
  void el.offsetWidth;
  el.classList.add(cls);
}

// Arrows, WASD, numpad and vi-keys all move; roguelike players expect all three.
const KEY_DIRS = {
  ArrowUp: [0, -1], ArrowDown: [0, 1], ArrowLeft: [-1, 0], ArrowRight: [1, 0],
  w: [0, -1], s: [0, 1], a: [-1, 0], d: [1, 0],
  q: [-1, -1], e: [1, -1], z: [-1, 1], c: [1, 1],
  k: [0, -1], j: [0, 1], h: [-1, 0], l: [1, 0],
  y: [-1, -1], u: [1, -1], b: [-1, 1], n: [1, 1],
  8: [0, -1], 2: [0, 1], 4: [-1, 0], 6: [1, 0],
  7: [-1, -1], 9: [1, -1], 1: [-1, 1], 3: [1, 1],
};

document.addEventListener('keydown', (e) => {
  if (e.ctrlKey || e.metaKey || e.altKey) return;
  const k = e.key.length === 1 ? e.key.toLowerCase() : e.key;

  // Digits select pantry slots unless they arrive from the numpad (then they move).
  if (/^[1-8]$/.test(k) && e.location !== KeyboardEvent.DOM_KEY_LOCATION_NUMPAD) {
    update(selectSlot(state, Number(k) - 1));
    e.preventDefault();
    return;
  }
  const dir = KEY_DIRS[k];
  if (dir) {
    update(stepPlayer(state, dir[0], dir[1]));
    e.preventDefault();
  } else if (k === ' ' || k === '.' || k === '5') {
    update(waitTurn(state));
    e.preventDefault();
  } else if (k === 'Escape') {
    update(selectSlot(state, null));
  } else if (k === 'r') {
    update(newGame());
  }
});

document.getElementById('pantry').addEventListener('click', (e) => {
  const slot = e.target.closest('[data-slot]');
  if (slot) update(selectSlot(state, Number(slot.dataset.slot)));
});
document.getElementById('btn-wait').addEventListener('click', () => update(waitTurn(state)));
document.getElementById('btn-restart').addEventListener('click', () => update(newGame()));

function readBest() {
  try {
    return Number(localStorage.getItem(BEST_KEY)) || 0;
  } catch {
    return 0;
  }
}
function writeBest(value) {
  try {
    localStorage.setItem(BEST_KEY, String(value));
  } catch {
    /* private mode etc. — best score is a nicety */
  }
}

decorateHud();
render(state, tiles, ui);

import { MODE } from './game/constants.js';
import { newGame, movePlayer, setMode, nextLevel } from './game/state.js';
import { createBoard, decorateHud, render } from './render.js';

let state = newGame();

const boardEl = document.getElementById('board');
const squares = createBoard(boardEl, (x, y) => update(movePlayer(state, { x, y })));

function update(next) {
  state = next;
  render(state, squares);
}

document.getElementById('btn-pepper').addEventListener('click', () => update(setMode(state, MODE.PEPPER)));
document.getElementById('btn-curry').addEventListener('click', () => update(setMode(state, MODE.CURRY)));
document.getElementById('btn-next').addEventListener('click', () => update(nextLevel(state)));
document.getElementById('btn-restart').addEventListener('click', () => update(newGame()));

document.addEventListener('keydown', (e) => {
  if (e.key === '1') update(setMode(state, MODE.PEPPER));
  else if (e.key === '2') update(setMode(state, MODE.CURRY));
  else if (e.key === 'Escape') update(setMode(state, MODE.NORMAL));
  else if (e.key === 'Enter' || e.key === ' ') update(nextLevel(state));
  else if (e.key.toLowerCase() === 'r') update(newGame());
});

decorateHud();
render(state, squares);

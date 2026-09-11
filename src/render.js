import { MAP_W, MAP_H, PANTRY_SIZE, TILE, STATUS } from './game/constants.js';
import { idx, key, tileAt } from './game/fov.js';
import { guestAt, itemAt, isPlayerAt, isKingAt, threatenedSquares } from './game/moves.js';
import { legalMoves, floorName, visibleSet } from './game/state.js';
import { FLOORS } from './game/levels.js';
import { GUESTS } from './game/guests.js';
import { itemName } from './game/items.js';
import { playerSprite, kingSprite, guestSprite, itemSprite, stairsSprite, itemIcon, guestIcon, heartIcon } from './art/sprites.js';

const $ = (id) => document.getElementById(id);
const esc = (s) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

/** Build the tile elements once; later renders only update classes/content. */
export function createBoard(boardEl, onTileClick) {
  boardEl.style.setProperty('--cols', MAP_W);
  boardEl.style.setProperty('--rows', MAP_H);
  const tiles = [];
  for (let y = 0; y < MAP_H; y++) {
    for (let x = 0; x < MAP_W; x++) {
      const el = document.createElement('div');
      el.className = 't';
      el.dataset.x = x;
      el.dataset.y = y;
      el.addEventListener('click', () => onTileClick(x, y));
      boardEl.appendChild(el);
      tiles.push(el);
    }
  }
  return tiles;
}

/** Static HUD bits that never change between renders. */
export function decorateHud() {
  $('guest-guide').innerHTML = Object.entries(GUESTS)
    .map(([type, g]) => `<div class="guide-row">${guestIcon(type)}<span><b>${esc(g.name)}</b> — ${esc(g.howMoves)}</span></div>`)
    .join('');
}

export function render(state, tiles, ui = {}) {
  const { map } = state;
  const visible = visibleSet(state);
  const threats = threatenedSquares(state);
  const moves = new Map(legalMoves(state).map((m) => [key(m.x, m.y), m]));
  const bubbles = new Map(state.bubbles.map((b) => [key(b.x, b.y), b.text]));
  const playing = state.status === STATUS.PLAYING;

  for (const el of tiles) {
    const x = Number(el.dataset.x);
    const y = Number(el.dataset.y);
    const k = key(x, y);
    const seen = state.seen[idx(map, x, y)] === 1;
    const lit = visible.has(k);
    const tile = tileAt(map, x, y);

    let cls = 't';
    let html = '';
    if (seen) {
      cls += tile === TILE.WALL ? ' wall' : (x + y) % 2 === 0 ? ' floor dark' : ' floor light';
      if (!lit) cls += ' dim';
      const move = playing && moves.get(k);
      if (move) cls += ' move';

      // Paper layers, back to front: threat tissue, stairs, item, piece, marker, bubble.
      if (lit && threats.has(k) && tile !== TILE.WALL) html += '<span class="layer threat"></span>';
      if (tile === TILE.STAIRS) html += stairsSprite(x, y);
      const item = itemAt(state, x, y);
      if (item) html += itemSprite(item.type, x, y);
      if (isPlayerAt(state, x, y)) html += playerSprite(x, y);
      else if (lit) {
        const guest = guestAt(state, x, y);
        if (guest) html += guestSprite(guest.type, x, y) + hpPips(guest);
        else if (isKingAt(state, x, y)) html += kingSprite(x, y);
      }
      if (move && (move.guest || move.king)) html += `<span class="layer ${move.king ? 'serve' : 'bump'}"></span>`;
      const bubble = lit && bubbles.get(k);
      if (bubble) html += `<span class="bubble">${esc(bubble)}</span>`;
    }
    if (el.className !== cls) el.className = cls;
    if (el.innerHTML !== html) el.innerHTML = html;
  }

  renderHud(state, ui);
}

function hpPips(guest) {
  const max = GUESTS[guest.type].hp;
  if (guest.hp >= max) return '';
  return `<span class="pips">${'●'.repeat(guest.hp)}${'○'.repeat(max - guest.hp)}</span>`;
}

function renderHud(state, ui) {
  $('floor').textContent = `${state.floor + 1}/${FLOORS.length} · ${floorName(state)}`;
  $('patience').innerHTML = Array.from({ length: state.maxPatience }, (_, i) => heartIcon(i < state.patience)).join('');
  $('tips').textContent = state.tips;
  $('best').textContent = ui.best ? `(best ${ui.best})` : '';
  $('turn').textContent = state.turn;
  $('utensil').textContent = `${state.utensil.name} · ${state.utensil.dmg} dmg`;
  $('apron').textContent = state.armor > 0 ? `${state.armor} padding` : 'cotton, no padding';

  const have = new Set(state.pantry);
  $('order').innerHTML = state.recipe
    .map((type) => `<span class="chip ${have.has(type) ? 'have' : 'missing'}" title="${esc(itemName(type))}">${itemIcon(type)}</span>`)
    .join('');

  const playing = state.status === STATUS.PLAYING;
  $('pantry').innerHTML = Array.from({ length: PANTRY_SIZE }, (_, i) => {
    const type = state.pantry[i];
    if (!type) return '<span class="slot empty"></span>';
    const held = state.held === i;
    return `<button type="button" class="slot ${held ? 'held' : ''}" data-slot="${i}" title="${esc(itemName(type))}" ${playing ? '' : 'disabled'}>${itemIcon(type)}<span class="num">${i + 1}</span></button>`;
  }).join('');

  const logEl = $('log');
  logEl.innerHTML = state.log
    .map((line) => `<p class="${line.kind}${line.turn < state.turn ? ' old' : ''}">${esc(line.text)}</p>`)
    .join('');
  logEl.scrollTop = logEl.scrollHeight;

  const ending = $('ending');
  if (!playing) {
    const meal = state.ending;
    const title = state.status === STATUS.WON ? 'Dinner is served!' : meal ? 'Dismissed.' : 'You quit.';
    const rank = meal ? `<div class="rank">Rank ${meal.rank}</div>` : '';
    const plate = meal && meal.served.length ? `<div class="plate">${meal.served.map(itemIcon).join('')}</div>` : '';
    ending.innerHTML = `<div class="ending-title">${title}</div>${rank}${plate}<div class="final">${state.tips} tips</div>`;
    ending.hidden = false;
  } else {
    ending.hidden = true;
  }

  $('btn-wait').disabled = !playing;
}


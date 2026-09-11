import { PIECE, PICKUP } from '../game/constants.js';

// Paper cut-out art: flat silhouettes (viewBox 0 0 64 64) drawn as inline SVG.
// Chess pieces are kitchen-twisted: knight = ladle, bishop = spice jar,
// rook = pepper grinder, queen = chef's toque. Fill colour comes from CSS.
const SILHOUETTE = {
  [PIECE.PAWN]: '<circle cx="32" cy="17" r="9"/><rect x="23" y="26" width="18" height="5" rx="2"/><path d="M26 31h12l5 18H21z"/><rect x="15" y="49" width="34" height="8" rx="2"/>',
  [PIECE.KNIGHT]: '<path d="M16 42h28a14 14 0 0 1-28 0z"/><path d="M35 42V17a9 9 0 0 1 18 0v6h-7v-6a2 2 0 0 0-4 0v25z"/>',
  [PIECE.BISHOP]: '<circle cx="32" cy="9" r="3"/><path d="M20 22a12 12 0 0 1 24 0z"/><rect x="18" y="21" width="28" height="6" rx="2"/><rect x="21" y="27" width="22" height="30" rx="4"/>',
  [PIECE.ROOK]: '<circle cx="32" cy="10" r="5"/><rect x="27" y="13" width="10" height="6"/><rect x="21" y="19" width="22" height="8" rx="2"/><rect x="23" y="27" width="18" height="30" rx="3"/>',
  [PIECE.QUEEN]: '<circle cx="22" cy="19" r="10"/><circle cx="32" cy="15" r="12"/><circle cx="42" cy="19" r="10"/><rect x="18" y="27" width="28" height="8" rx="2"/><path d="M24 35h16l5 17H19z"/><rect x="15" y="52" width="34" height="6" rx="2"/>',
};

// The player pawn wears a chili leaf.
const LEAF = '<path class="leaf" d="M32 8c1-5 5-7 10-6-2 3-5 5-8 6z"/>';

const PICKUP_ART = {
  [PICKUP.PEPPER]:
    '<path class="chili" d="M46 14c0 10-5 20-14 28-7 6-16 10-24 12-2 .5-3-2-1-3 8-3 15-8 21-15 6-7 10-14 12-22z"/><path class="leaf" d="M44 15c2-5 6-7 11-6-3 2-5 4-6 8z"/>',
  [PICKUP.CURRY]:
    '<path class="mound" d="M12 34c4-9 12-13 20-13s16 4 20 13z"/><path class="dish" d="M8 34h48c0 13-10 22-24 22S8 47 8 34z"/><path class="steam" d="M26 10c-3 4 3 6 0 10M38 8c-3 4 3 6 0 10"/>',
};

const HEART = '<path d="M12 21s-7.5-4.6-9.6-9.4C.9 8.2 3.2 4.5 6.9 4.5c2 0 3.6 1.1 5.1 2.9 1.5-1.8 3.1-2.9 5.1-2.9 3.7 0 6 3.7 4.5 7.1C19.5 16.4 12 21 12 21z"/>';

const wrap = (inner, cls, extraAttrs = '') =>
  `<svg class="${cls}" viewBox="0 0 64 64" aria-hidden="true" ${extraAttrs}>${inner}</svg>`;

/** Deterministic per-square tilt so the paper jitter is stable between renders. */
export function tiltFor(x, y) {
  return ((x * 7 + y * 13) % 9) - 4;
}

export function playerSprite(x, y) {
  return wrap(SILHOUETTE[PIECE.PAWN] + LEAF, 'piece player', `style="--tilt:${tiltFor(x, y)}deg"`);
}

export function enemySprite(type, x, y) {
  const art = SILHOUETTE[type];
  if (!art) throw new Error(`No sprite for piece type "${type}"`);
  return wrap(art, 'piece enemy', `style="--tilt:${tiltFor(x, y)}deg"`);
}

export function pickupSprite(type, x, y) {
  const art = PICKUP_ART[type];
  if (!art) throw new Error(`No sprite for pickup type "${type}"`);
  return wrap(art, `pickup ${type}`, `style="--tilt:${tiltFor(x, y)}deg"`);
}

/** Small icon (for HUD chips) — no tilt, sized by CSS. */
export function pickupIcon(type) {
  return wrap(PICKUP_ART[type], `icon ${type}`);
}

export function heartIcon(filled) {
  return `<svg class="heart ${filled ? 'full' : 'empty'}" viewBox="0 0 24 24" aria-hidden="true">${HEART}</svg>`;
}

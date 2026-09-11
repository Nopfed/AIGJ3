import { GUEST, ITEM } from '../game/constants.js';

// Paper cut-out art: flat silhouettes (viewBox 0 0 64 64) drawn as inline SVG.
// Guests are kitchen-twisted chess pieces: knight = ladle, bishop = spice jar,
// rook = pepper grinder, queen = chef's toque. Fill colours come from CSS.
const GUEST_ART = {
  [GUEST.PAGE]: '<circle cx="32" cy="17" r="9"/><rect x="23" y="26" width="18" height="5" rx="2"/><path d="M26 31h12l5 18H21z"/><rect x="15" y="49" width="34" height="8" rx="2"/>',
  [GUEST.KNIGHT]: '<path d="M16 42h28a14 14 0 0 1-28 0z"/><path d="M35 42V17a9 9 0 0 1 18 0v6h-7v-6a2 2 0 0 0-4 0v25z"/>',
  [GUEST.BISHOP]: '<circle cx="32" cy="9" r="3"/><path d="M20 22a12 12 0 0 1 24 0z"/><rect x="18" y="21" width="28" height="6" rx="2"/><rect x="21" y="27" width="22" height="30" rx="4"/>',
  [GUEST.ROOK]: '<circle cx="32" cy="10" r="5"/><rect x="27" y="13" width="10" height="6"/><rect x="21" y="19" width="22" height="8" rx="2"/><rect x="23" y="27" width="18" height="30" rx="3"/>',
  [GUEST.QUEEN]: '<circle cx="22" cy="19" r="10"/><circle cx="32" cy="15" r="12"/><circle cx="42" cy="19" r="10"/><rect x="18" y="27" width="28" height="8" rx="2"/><path d="M24 35h16l5 17H19z"/><rect x="15" y="52" width="34" height="6" rx="2"/>',
};

// The chef: a pawn in a toque, chili pinned to the band.
const CHEF =
  '<circle cx="23" cy="15" r="7"/><circle cx="32" cy="11" r="8"/><circle cx="41" cy="15" r="7"/><rect x="19" y="14" width="26" height="10"/>' +
  '<rect x="18" y="23" width="28" height="5" rx="2"/><circle cx="32" cy="34" r="6"/><path d="M25 39h14l5 13H20z"/><rect x="16" y="52" width="32" height="6" rx="2"/>' +
  '<path class="chili" d="M47 21c-1 4-4 7-8 8 2-3 3-6 4-9z"/>';

// The King: a piece wearing an actual crown, waiting at the head of the table.
const KING =
  '<path class="crown" d="M17 24l4-14 11 8 11-8 4 14z"/><rect x="20" y="24" width="24" height="7" rx="2"/>' +
  '<path d="M25 31h14l6 20H19z"/><rect x="15" y="51" width="34" height="7" rx="2"/>';

const ITEM_ART = {
  [ITEM.CHILI]:
    '<path class="chili" d="M46 14c0 10-5 20-14 28-7 6-16 10-24 12-2 .5-3-2-1-3 8-3 15-8 21-15 6-7 10-14 12-22z"/><path class="leaf" d="M44 15c2-5 6-7 11-6-3 2-5 4-6 8z"/>',
  [ITEM.CURRY]:
    '<path class="mound" d="M12 34c4-9 12-13 20-13s16 4 20 13z"/><path class="dish" d="M8 34h48c0 13-10 22-24 22S8 47 8 34z"/><path class="steam" d="M26 10c-3 4 3 6 0 10M38 8c-3 4 3 6 0 10"/>',
  [ITEM.RICE]:
    '<ellipse class="rice" cx="32" cy="32" rx="20" ry="9"/><path class="bowl" d="M10 34h44c0 12-9 20-22 20S10 46 10 34z"/><path class="steam" d="M28 12c-3 4 3 6 0 10M38 10c-3 4 3 6 0 10"/>',
  [ITEM.CHICKEN]:
    '<g transform="rotate(-40 32 32)"><rect class="bone" x="36" y="29" width="18" height="6" rx="3"/><circle class="bone" cx="55" cy="28" r="4"/><circle class="bone" cx="55" cy="36" r="4"/><ellipse class="meat" cx="26" cy="32" rx="17" ry="12"/></g>',
  [ITEM.ONION]:
    '<path class="leaf" d="M30 6l2 10 2-10z"/><path class="onion" d="M32 12c-2 6-5 9-9 13a15 15 0 1 0 18 0c-4-4-7-7-9-13z"/><path class="onion-line" d="M32 22v30M26 27c-2 8-2 16 0 22M38 27c2 8 2 16 0 22"/>',
  [ITEM.BREAD]:
    '<path class="bread" d="M12 42a20 15 0 0 1 40 0v6a4 4 0 0 1-4 4H16a4 4 0 0 1-4-4z"/><path class="score" d="M20 36l6-7M30 36l6-7M40 36l6-7"/>',
  [ITEM.TEA]:
    '<path class="pot" d="M18 26h28a4 4 0 0 1 4 4v10a14 14 0 0 1-14 14h-8A14 14 0 0 1 14 40V30a4 4 0 0 1 4-4z"/><path class="pot" d="M48 32h5a5 5 0 0 1 0 10h-5z"/><path class="pot" d="M26 26c0-6 3-9 6-9s6 3 6 9z"/><path class="steam" d="M32 6c-3 4 3 6 0 10"/>',
  [ITEM.ROLLING_PIN]:
    '<g transform="rotate(-35 32 32)"><rect class="handle" x="2" y="28" width="12" height="8" rx="3"/><rect class="handle" x="50" y="28" width="12" height="8" rx="3"/><rect class="wood" x="14" y="25" width="36" height="14" rx="3"/></g>',
  [ITEM.CLEAVER]:
    '<g transform="rotate(-30 32 32)"><rect class="handle" x="40" y="25" width="20" height="8" rx="3"/><path class="blade" d="M8 18h32v22H12a4 4 0 0 1-4-4z"/><circle class="hole" cx="15" cy="24" r="2"/></g>',
  [ITEM.APRON]:
    '<path class="tie" d="M22 14l-9 12M42 14l9 12"/><path class="apron" d="M22 8h20v10a10 10 0 0 0 10 10v28H12V28a10 10 0 0 0 10-10z"/><rect class="pocket" x="26" y="34" width="12" height="9" rx="1"/>',
};

const STAIRS = '<path class="steps" d="M6 56V44h12V32h12V20h12V8h16v48z"/>';

const HEART = '<path d="M12 21s-7.5-4.6-9.6-9.4C.9 8.2 3.2 4.5 6.9 4.5c2 0 3.6 1.1 5.1 2.9 1.5-1.8 3.1-2.9 5.1-2.9 3.7 0 6 3.7 4.5 7.1C19.5 16.4 12 21 12 21z"/>';

const wrap = (inner, cls, extraAttrs = '') =>
  `<svg class="${cls}" viewBox="0 0 64 64" aria-hidden="true" ${extraAttrs}>${inner}</svg>`;

/** Deterministic per-tile tilt so the paper jitter is stable between renders. */
export function tiltFor(x, y) {
  return ((x * 7 + y * 13) % 9) - 4;
}

const tilt = (x, y) => `style="--tilt:${tiltFor(x, y)}deg"`;

export const playerSprite = (x, y) => wrap(CHEF, 'piece player', tilt(x, y));
export const kingSprite = (x, y) => wrap(KING, 'piece king', tilt(x, y));

export function guestSprite(type, x, y) {
  const art = GUEST_ART[type];
  if (!art) throw new Error(`No sprite for guest type "${type}"`);
  return wrap(art, 'piece guest', tilt(x, y));
}

export function itemSprite(type, x, y) {
  const art = ITEM_ART[type];
  if (!art) throw new Error(`No sprite for item type "${type}"`);
  return wrap(art, `item ${type}`, tilt(x, y));
}

export const stairsSprite = (x, y) => wrap(STAIRS, 'stairs', tilt(x, y));

/** Small icon (for HUD chips) — no tilt, sized by CSS. */
export function itemIcon(type) {
  const art = ITEM_ART[type];
  if (!art) throw new Error(`No icon for item type "${type}"`);
  return wrap(art, `icon ${type}`);
}

export function guestIcon(type) {
  const art = GUEST_ART[type];
  if (!art) throw new Error(`No icon for guest type "${type}"`);
  return wrap(art, 'icon guest');
}

export function heartIcon(filled) {
  return `<svg class="heart ${filled ? 'full' : 'empty'}" viewBox="0 0 24 24" aria-hidden="true">${HEART}</svg>`;
}

import {
  FOV_RADIUS, HEARING_RANGE, MAX_PATIENCE, PANTRY_SIZE, MAX_ARMOR, FLOOR_TIPS, TILE, STATUS,
} from './constants.js';
import { computeVisible, distanceMap, firstStep, idx, key, tileAt, chebyshev } from './fov.js';
import { playerMoves, guestAt, itemAt } from './moves.js';
import { chooseGuestAction, guestOrder } from './ai.js';
import { generateFloor, FLOORS } from './levels.js';
import { guestDef } from './guests.js';
import { GEAR, ROYAL_RECIPE, STARTING_UTENSIL, isIngredient, itemName, scoreMeal } from './items.js';

const LOG_LINES = 10;
const clone = (state) => JSON.parse(JSON.stringify(state));
const msg = (text, kind = 'info') => ({ text, kind });
const cap = (s) => s.charAt(0).toUpperCase() + s.slice(1);

export function newGame(seed = Date.now() % 1_000_000) {
  const base = {
    seed,
    floor: 0,
    turn: 0,
    status: STATUS.PLAYING,
    tips: 0,
    patience: MAX_PATIENCE,
    maxPatience: MAX_PATIENCE,
    utensil: { ...STARTING_UTENSIL },
    armor: 0,
    pantry: [],
    held: null,
    recipe: ROYAL_RECIPE.parts.slice(),
    log: [
      msg(`Tonight the King desires a ${ROYAL_RECIPE.name}: ${ROYAL_RECIPE.parts.map(itemName).join(', ')}.`, 'good'),
      msg(`Gather what you can and find him in the Dining Hall. You enter ${FLOORS[0].name}.`),
    ],
    bubbles: [],
    ending: null,
  };
  return stamp(loadFloor(base, 0));
}

function loadFloor(state, floorIndex) {
  const layout = generateFloor(floorIndex, state.seed);
  const next = {
    ...state,
    floor: floorIndex,
    map: layout.map,
    player: layout.player,
    guests: layout.guests,
    items: layout.items,
    stairs: layout.stairs,
    king: layout.king,
    seen: new Array(layout.map.w * layout.map.h).fill(0),
    bubbles: [],
  };
  reveal(next);
  return next;
}

export const floorName = (state) => FLOORS[state.floor].name;
export const isLastFloor = (state) => state.floor >= FLOORS.length - 1;
export const visibleSet = (state) => computeVisible(state.map, state.player, FOV_RADIUS);

/** Adjacent floor tiles the chef can step to (or bump into). */
export function legalMoves(state) {
  if (state.status !== STATUS.PLAYING) return [];
  return playerMoves(state);
}

/** Light up what the chef can see; guests that come into view notice him. */
function reveal(state) {
  const visible = visibleSet(state);
  for (const k of visible) {
    const [x, y] = k.split(',').map(Number);
    state.seen[idx(state.map, x, y)] = 1;
  }
  for (const guest of state.guests) {
    if (!guest.aware && visible.has(key(guest.x, guest.y))) notice(state, guest, 'notices you');
  }
  return visible;
}

function notice(state, guest, how) {
  guest.aware = true;
  guest.noticedOn = state.turn; // a guest who has just noticed you loses this turn
  state.bubbles.push({ x: guest.x, y: guest.y, text: '!' });
  state.log.push(msg(`${cap(guestDef(guest.type).name)} ${how}.`, 'guest'));
}

function beginTurn(state) {
  const next = clone(state);
  next.turn += 1;
  // Keep a short history; lines carry the turn they happened on so the HUD can fade old ones.
  next.log = next.log.slice(-LOG_LINES);
  next.bubbles = [];
  return next;
}

/** Stamp new log lines with the turn they were written on. */
function stamp(state) {
  for (const line of state.log) if (line.turn === undefined) line.turn = state.turn;
  return state;
}

function endTurn(next) {
  reveal(next);
  runGuestTurn(next);
  if (next.status === STATUS.PLAYING) reveal(next);
  return stamp(next);
}

/** Step one tile in a direction: move, pick up, descend, bump a guest, or serve the King. */
export function stepPlayer(state, dx, dy) {
  if (state.status !== STATUS.PLAYING) return state;
  const move = playerMoves(state).find((m) => m.dx === dx && m.dy === dy);
  if (!move) return state;

  const next = beginTurn(state);
  if (move.guest) {
    const guest = guestAt(next, move.x, move.y);
    if (next.held !== null && next.pantry[next.held] !== undefined) serveGuest(next, guest);
    else attackGuest(next, guest);
    return endTurn(next);
  }
  if (move.king) return stamp(serveKing(next));

  next.player = { x: move.x, y: move.y };
  pickUp(next);
  if (tileAt(next.map, move.x, move.y) === TILE.STAIRS) return stamp(descend(next));
  return endTurn(next);
}

export function waitTurn(state) {
  if (state.status !== STATUS.PLAYING) return state;
  const next = beginTurn(state);
  next.log.push(msg('You catch your breath.'));
  return endTurn(next);
}

/** Hold (or put down) a pantry ingredient to serve to the next guest you bump. Free action. */
export function selectSlot(state, slot) {
  if (state.status !== STATUS.PLAYING) return state;
  if (slot !== null && (slot < 0 || slot >= state.pantry.length)) return state;
  const next = clone(state);
  next.held = slot === null || next.held === slot ? null : slot;
  return next;
}

/** Mouse helper: take one step along a known path toward a tile. */
export function stepToward(state, x, y) {
  if (state.status !== STATUS.PLAYING) return state;
  const { player, map } = state;
  if (player.x === x && player.y === y) return waitTurn(state);
  if (chebyshev(player, { x, y }) === 1) return stepPlayer(state, x - player.x, y - player.y);
  const passable = (tx, ty) => state.seen[idx(map, tx, ty)] === 1 && !guestAt(state, tx, ty) && !(state.king && state.king.x === tx && state.king.y === ty);
  const step = firstStep(map, player, { x, y }, passable);
  if (!step) return state;
  return stepPlayer(state, step.dx, step.dy);
}

function attackGuest(next, guest) {
  const def = guestDef(guest.type);
  guest.hp -= next.utensil.dmg;
  if (!guest.aware) {
    guest.aware = true;
    guest.noticedOn = next.turn - 1; // being whacked wakes anyone up
  }
  if (guest.hp <= 0) {
    next.guests = next.guests.filter((g) => g.id !== guest.id);
    next.tips += def.tips;
    next.log.push(msg(`You send ${def.name} packing with the ${next.utensil.name}. +${def.tips} tips`, 'good'));
  } else {
    next.log.push(msg(`You whack ${def.name} with the ${next.utensil.name}. (${guest.hp} left)`));
  }
}

function serveGuest(next, guest) {
  const def = guestDef(guest.type);
  const [type] = next.pantry.splice(next.held, 1);
  next.held = null;
  next.guests = next.guests.filter((g) => g.id !== guest.id);
  const reward = def.tips + 10;
  next.tips += reward;
  next.bubbles.push({ x: guest.x, y: guest.y, text: 'Mmm!' });
  next.log.push(msg(`You hand ${def.name} a ${itemName(type)}. ${def.short} wanders off, delighted. +${reward} tips`, 'good'));
}

function pickUp(next) {
  const { x, y } = next.player;
  const item = itemAt(next, x, y);
  if (!item) return;
  const name = itemName(item.type);

  if (isIngredient(item.type)) {
    if (next.pantry.length >= PANTRY_SIZE) {
      next.log.push(msg(`Your pantry is full; the ${name} stays on the floor.`));
      return;
    }
    next.pantry.push(item.type);
    const wanted = next.recipe.includes(item.type) ? ' The King asked for this!' : '';
    next.log.push(msg(`You pocket a ${name}.${wanted}`, 'good'));
  } else {
    const gear = GEAR[item.type];
    if (gear.kind === 'tea') {
      const before = next.patience;
      next.patience = Math.min(next.maxPatience, next.patience + gear.heal);
      next.log.push(msg(`A ${name}. Ahh. +${next.patience - before} patience`, 'good'));
    } else if (gear.kind === 'utensil') {
      if (gear.dmg > next.utensil.dmg) {
        next.utensil = { name: gear.name, dmg: gear.dmg };
        next.log.push(msg(`You take up the ${name}. It hits for ${gear.dmg}.`, 'good'));
      } else {
        next.tips += 5;
        next.log.push(msg(`A ${name}, but your ${next.utensil.name} is better. Sold to a page. +5 tips`));
      }
    } else if (gear.kind === 'apron') {
      if (next.armor < MAX_ARMOR) {
        next.armor += gear.armor;
        next.log.push(msg(`You tie on a ${name}. Jabs hurt ${next.armor} less.`, 'good'));
      } else {
        next.tips += 5;
        next.log.push(msg('Another apron? You are wearing enough. +5 tips'));
      }
    }
  }
  next.items = next.items.filter((i) => i !== item);
}

function descend(next) {
  next.tips += FLOOR_TIPS;
  const name = FLOORS[next.floor + 1].name;
  next.log.push(msg(`You take the stairs down into ${name}. +${FLOOR_TIPS} tips`, 'good'));
  return loadFloor(next, next.floor + 1);
}

function serveKing(next) {
  const meal = scoreMeal(next.pantry, { name: ROYAL_RECIPE.name, parts: next.recipe });
  next.tips += meal.tips;
  next.ending = meal;
  next.held = null;
  next.status = meal.rank === 'F' ? STATUS.LOST : STATUS.WON;
  const served = meal.served.length ? meal.served.map(itemName).join(', ') : 'nothing at all';
  next.log.push(msg(`You bow and present the King with ${served}.`));
  next.log.push(msg(meal.verdict, next.status === STATUS.WON ? 'good' : 'hurt'));
  if (meal.tips > 0) next.log.push(msg(`+${meal.tips} tips. Rank ${meal.rank}.`, 'good'));
  return next;
}

function runGuestTurn(next) {
  const dist = distanceMap(next.map, next.player);
  for (const ordered of guestOrder(next)) {
    const guest = next.guests.find((g) => g.id === ordered.id);
    if (!guest) continue;

    if (!guest.aware) {
      const d = dist[idx(next.map, guest.x, guest.y)];
      if (d !== -1 && d <= HEARING_RANGE) notice(next, guest, 'hears you clattering about');
      continue;
    }
    if (guest.noticedOn === next.turn) continue;
    if (guest.restingOn === next.turn) {
      next.bubbles.push({ x: guest.x, y: guest.y, text: 'huff…' });
      continue;
    }

    const action = chooseGuestAction(next, guest, dist);
    if (!action) continue;
    if (action.kind === 'move') {
      guest.x = action.x;
      guest.y = action.y;
      continue;
    }
    guestAttacks(next, guest);
    if (next.status !== STATUS.PLAYING) return;
  }
}

function guestAttacks(next, guest) {
  const def = guestDef(guest.type);
  const name = cap(def.name);
  const line = def.lines[(next.turn * 7 + guest.id * 3) % def.lines.length];

  if (def.rests) guest.restingOn = next.turn + 1;

  if (def.attack === 'jab') {
    const dmg = Math.max(0, def.dmg - next.armor);
    next.bubbles.push({ x: guest.x, y: guest.y, text: '!' });
    hurt(next, dmg, `${name} ${line}.`);
  } else if (def.attack === 'remark') {
    next.bubbles.push({ x: guest.x, y: guest.y, text: line.replace(/"/g, '') });
    hurt(next, def.dmg, `${name} sneers: ${line}`);
  } else if (def.attack === 'demand') {
    next.bubbles.push({ x: guest.x, y: guest.y, text: line.replace(/"/g, '') });
    if (next.pantry.length > 0) {
      const slot = (next.turn + guest.id) % next.pantry.length;
      const [stolen] = next.pantry.splice(slot, 1);
      if (next.held !== null) {
        if (next.held === slot) next.held = null;
        else if (next.held > slot) next.held -= 1;
      }
      // He got his snack; he is done with you.
      next.guests = next.guests.filter((g) => g.id !== guest.id);
      next.log.push(msg(`${name} snaps ${line} and snatches your ${itemName(stolen)}, then waddles off with it.`, 'hurt'));
    } else {
      hurt(next, Math.max(0, def.dmg - next.armor), `${name} snaps ${line} and rattles his grinder in your face.`);
    }
  }
}

function hurt(next, dmg, text) {
  if (dmg <= 0) {
    next.log.push(msg(`${text} Your apron takes it.`));
    return;
  }
  next.patience -= dmg;
  next.log.push(msg(`${text} -${dmg} patience`, 'hurt'));
  if (next.patience <= 0) {
    next.patience = 0;
    next.status = STATUS.LOST;
    next.log.push(msg('You have had ENOUGH. You tear off your apron and storm out of the castle.', 'hurt'));
  }
}

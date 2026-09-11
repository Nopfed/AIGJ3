import { GUEST } from './constants.js';

/**
 * Guest roster. `attack` is how they hurt the chef:
 *  - jab:    physical, softened by the apron
 *  - remark: verbal, ignores the apron entirely
 *  - demand: snatches an ingredient from the pantry and leaves satisfied, or jabs if it's empty
 * `rests` guests skip the turn after they land a hit (a big swing, a long tirade).
 */
export const GUESTS = {
  [GUEST.PAGE]: {
    name: 'the Peckish Page',
    short: 'Page',
    hp: 1,
    dmg: 1,
    attack: 'jab',
    tips: 5,
    lines: ['pokes you with a fork', 'kicks your shin', 'flicks a pea at your eye', 'tugs your apron strings'],
    howMoves: 'Steps straight, pokes diagonally.',
  },
  [GUEST.KNIGHT]: {
    name: 'Sir Ladle',
    short: 'Knight',
    hp: 2,
    dmg: 2,
    attack: 'jab',
    rests: true, // winded after a hit: skips the next turn
    tips: 10,
    lines: ['bonks you with his ladle', 'charges, ladle first', 'clangs his ladle off your head', 'vaults the table and clips you'],
    howMoves: 'Jumps in an L, even over walls. Harmless up close.',
  },
  [GUEST.BISHOP]: {
    name: 'the Bishop of Basil',
    short: 'Bishop',
    hp: 2,
    dmg: 1,
    attack: 'remark',
    tips: 10,
    lines: ['"Underseasoned. Like your career."', '"Is that a stain on your apron?"', '"I have had better soup from a boot."', '"You call this a kitchen?"'],
    howMoves: 'Slides diagonally. Sneers from across the room.',
  },
  [GUEST.ROOK]: {
    name: 'Baron Peppermill',
    short: 'Baron',
    hp: 3,
    dmg: 1,
    attack: 'demand',
    tips: 15,
    lines: ['"Bread! I demand bread this instant!"', '"Where is my snack, chef?!"', '"I have been waiting an HOUR."', '"Feed me or fetch someone who will."'],
    howMoves: 'Slides straight. Snatches food from your pantry and leaves with it.',
  },
  [GUEST.QUEEN]: {
    name: 'Duchess Toque',
    short: 'Duchess',
    hp: 4,
    dmg: 2,
    attack: 'remark',
    rests: true, // needs a breath between tirades
    tips: 30,
    lines: ['"THIS is the chef? How quaint."', '"Off with his toque!"', '"I shall have you scrubbing pots by dawn."', '"My poodle cooks better than this."'],
    howMoves: 'Slides any direction. Devastating tongue, but pauses for breath.',
  },
};

export function guestDef(type) {
  const def = GUESTS[type];
  if (!def) throw new Error(`Unknown guest type "${type}"`);
  return def;
}

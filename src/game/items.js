import { ITEM } from './constants.js';

// Ingredients go in the pantry and end up on the King's plate (or in a guest's mouth).
export const INGREDIENTS = {
  [ITEM.CHILI]: { name: 'chili pepper', tips: 10 },
  [ITEM.CURRY]: { name: 'curry paste', tips: 10 },
  [ITEM.RICE]: { name: 'bowl of rice', tips: 10 },
  [ITEM.CHICKEN]: { name: 'chicken leg', tips: 10 },
  [ITEM.ONION]: { name: 'onion', tips: 10 },
  [ITEM.BREAD]: { name: 'bread roll', tips: 5 },
};

// Gear is used the moment you step on it (arcade rules, no inventory fiddling).
export const GEAR = {
  [ITEM.TEA]: { name: 'pot of tea', kind: 'tea', heal: 5 },
  [ITEM.ROLLING_PIN]: { name: 'rolling pin', kind: 'utensil', dmg: 2 },
  [ITEM.CLEAVER]: { name: 'cleaver', kind: 'utensil', dmg: 3 },
  [ITEM.APRON]: { name: 'leather apron', kind: 'apron', armor: 1 },
};

export const STARTING_UTENSIL = { name: 'wooden spoon', dmg: 1 };

// What the King actually asked for tonight.
export const ROYAL_RECIPE = {
  name: 'Royal Curry',
  parts: [ITEM.CHILI, ITEM.CURRY, ITEM.RICE, ITEM.CHICKEN, ITEM.ONION],
};

export const PART_TIPS = 20;
export const COMPLETE_TIPS = 100;

export const isIngredient = (type) => type in INGREDIENTS;
export const itemName = (type) => (INGREDIENTS[type] || GEAR[type]).name;

/**
 * Judge the meal made from everything in the pantry. Every ingredient earns its
 * tips, each recipe part present earns a bonus, and the full recipe earns the crown.
 */
export function scoreMeal(pantry, recipe = ROYAL_RECIPE) {
  const have = new Set(pantry);
  const parts = recipe.parts.filter((p) => have.has(p));
  const complete = parts.length === recipe.parts.length;
  let tips = pantry.reduce((sum, type) => sum + INGREDIENTS[type].tips, 0);
  tips += parts.length * PART_TIPS;
  if (complete) tips += COMPLETE_TIPS;

  let rank;
  let verdict;
  if (pantry.length === 0) {
    rank = 'F';
    verdict = 'You present an empty plate. The King stares at it, then at you. You are shown to the moat.';
  } else if (complete) {
    rank = 'S';
    verdict = `A perfect ${recipe.name}! The King weeps with joy and names you Chef Royal.`;
  } else if (parts.length >= 4) {
    rank = 'A';
    verdict = 'Nearly the Royal Curry. The King mops the plate with his sleeve and asks for seconds.';
  } else if (parts.length >= 2) {
    rank = 'B';
    verdict = 'A respectable stew. The King nods and dabs his beard.';
  } else {
    rank = 'C';
    verdict = 'The King pokes at it with a fork. "Well. It is food."';
  }
  return {
    served: pantry.slice(),
    parts,
    missing: recipe.parts.filter((p) => !have.has(p)),
    complete,
    tips,
    rank,
    verdict,
  };
}

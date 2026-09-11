export const MAP_W = 30;
export const MAP_H = 18;
export const FOV_RADIUS = 7;
export const HEARING_RANGE = 3; // path distance at which an unseen guest still notices you
export const MAX_PATIENCE = 12;
export const PANTRY_SIZE = 8;
export const MAX_ARMOR = 3;
export const FLOOR_TIPS = 25;

export const TILE = {
  WALL: '#',
  FLOOR: '.',
  STAIRS: '>',
};

// Guests are chess pieces; the chef doesn't know the rules but they do.
export const GUEST = {
  PAGE: 'page', // pawn
  KNIGHT: 'knight',
  BISHOP: 'bishop',
  ROOK: 'rook',
  QUEEN: 'queen',
};

export const ITEM = {
  CHILI: 'chili',
  CURRY: 'curry',
  RICE: 'rice',
  CHICKEN: 'chicken',
  ONION: 'onion',
  BREAD: 'bread',
  TEA: 'tea',
  ROLLING_PIN: 'rollingPin',
  CLEAVER: 'cleaver',
  APRON: 'apron',
};

export const STATUS = {
  PLAYING: 'playing',
  WON: 'won',
  LOST: 'lost',
};

// Eight-way steps; the chef moves like a roguelike hero, not a chess piece.
export const DIRS = [
  [1, 0], [1, 1], [0, 1], [-1, 1],
  [-1, 0], [-1, -1], [0, -1], [1, -1],
];

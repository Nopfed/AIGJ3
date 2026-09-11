export const BOARD_SIZE = 8;
export const MAX_HEARTS = 3;
export const MAX_CHARGES = 3;

export const PIECE = {
  PAWN: 'pawn',
  KNIGHT: 'knight',
  BISHOP: 'bishop',
  ROOK: 'rook',
  QUEEN: 'queen',
};

export const PICKUP = {
  PEPPER: 'pepper', // next move: knight jump
  CURRY: 'curry',   // next move: king step
};

export const MODE = {
  NORMAL: 'normal',
  PEPPER: 'pepper',
  CURRY: 'curry',
};

export const STATUS = {
  PLAYING: 'playing',
  LEVEL_CLEAR: 'levelClear',
  WON: 'won',
  LOST: 'lost',
};

import { describe, it, expect } from 'vitest';
import {
  playerSprite, kingSprite, guestSprite, itemSprite, stairsSprite, itemIcon, guestIcon, heartIcon, tiltFor,
} from '../src/art/sprites.js';
import { GUEST, ITEM } from '../src/game/constants.js';

describe('paper sprites', () => {
  it('has a silhouette for every guest type', () => {
    for (const type of Object.values(GUEST)) {
      expect(guestSprite(type, 0, 0)).toContain('class="piece guest"');
      expect(guestIcon(type)).toContain('class="icon guest"');
    }
  });

  it('has art for every item type', () => {
    for (const type of Object.values(ITEM)) {
      expect(itemSprite(type, 1, 1)).toContain(`class="item ${type}"`);
      expect(itemIcon(type)).toContain(`class="icon ${type}"`);
    }
  });

  it('throws on unknown types instead of rendering nothing', () => {
    expect(() => guestSprite('dragon', 0, 0)).toThrow();
    expect(() => itemSprite('salt', 0, 0)).toThrow();
    expect(() => itemIcon('salt')).toThrow();
  });

  it('dresses the chef in a toque with a chili and the King in a crown', () => {
    expect(playerSprite(0, 0)).toContain('class="chili"');
    expect(playerSprite(0, 0)).toContain('class="piece player"');
    expect(kingSprite(0, 0)).toContain('class="crown"');
    expect(stairsSprite(0, 0)).toContain('class="stairs"');
    expect(heartIcon(true)).toContain('full');
    expect(heartIcon(false)).toContain('empty');
  });

  it('tilt is deterministic per tile and within ±4°', () => {
    for (let x = 0; x < 30; x++) {
      for (let y = 0; y < 18; y++) {
        const t = tiltFor(x, y);
        expect(t).toBe(tiltFor(x, y));
        expect(Math.abs(t)).toBeLessThanOrEqual(4);
      }
    }
    expect(playerSprite(3, 2)).toContain(`--tilt:${tiltFor(3, 2)}deg`);
  });
});

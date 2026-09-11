import { describe, it, expect } from 'vitest';
import { playerSprite, enemySprite, pickupSprite, pickupIcon, heartIcon, tiltFor } from '../src/art/sprites.js';
import { PIECE, PICKUP } from '../src/game/constants.js';

describe('paper sprites', () => {
  it('has a silhouette for every enemy piece type', () => {
    for (const type of Object.values(PIECE)) {
      expect(enemySprite(type, 0, 0)).toContain('class="piece enemy"');
    }
  });

  it('throws on unknown types instead of rendering nothing', () => {
    expect(() => enemySprite('dragon', 0, 0)).toThrow();
    expect(() => pickupSprite('salt', 0, 0)).toThrow();
  });

  it('gives the player pawn its chili leaf', () => {
    expect(playerSprite(0, 0)).toContain('class="leaf"');
  });

  it('renders both pickups and HUD icons', () => {
    for (const type of Object.values(PICKUP)) {
      expect(pickupSprite(type, 1, 1)).toContain(`class="pickup ${type}"`);
      expect(pickupIcon(type)).toContain(`class="icon ${type}"`);
    }
    expect(heartIcon(true)).toContain('full');
    expect(heartIcon(false)).toContain('empty');
  });

  it('tilt is deterministic per square and within ±4°', () => {
    for (let x = 0; x < 8; x++) {
      for (let y = 0; y < 8; y++) {
        const t = tiltFor(x, y);
        expect(t).toBe(tiltFor(x, y));
        expect(Math.abs(t)).toBeLessThanOrEqual(4);
      }
    }
    expect(playerSprite(3, 2)).toContain(`--tilt:${tiltFor(3, 2)}deg`);
  });
});

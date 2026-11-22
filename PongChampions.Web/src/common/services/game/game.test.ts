import { describe, expect, it } from 'vitest';
import type { IGameState } from '@models/game';
import { gameService } from './gameService';
import { createSnapshotBuffer } from './snapshotBuffer';

function state(overrides: Partial<IGameState> = {}): IGameState {
  return {
    roomCode: 'ABC123',
    hostPaddleY: 0.5,
    guestPaddleY: 0.5,
    ball: { x: 0.5, y: 0.5, velocityX: 0.4, velocityY: 0.2 },
    hostScore: 0,
    guestScore: 0,
    isRunning: true,
    targetScore: 5,
    ...overrides,
  };
}

describe('gameService', () => {
  it('predicts ball travel and bounces it off the walls', () => {
    const ball = { x: 0.5, y: 0.97, velocityX: 0.4, velocityY: 0.2 };
    const predicted = gameService.predictBall(ball, 0.1);

    expect(predicted.x).toBeCloseTo(0.54);
    // 0.97 + 0.02 = 0.99 overshoots the 0.985 limit by 0.005.
    expect(predicted.y).toBeCloseTo(0.98);
  });

  it('keeps paddles on the board like the server', () => {
    expect(gameService.clampPaddleY(0)).toBe(0.12);
    expect(gameService.clampPaddleY(1)).toBe(0.88);
  });

  it('detects the serve pause and the winner', () => {
    expect(
      gameService.isServing(
        state({ ball: { x: 0.5, y: 0.5, velocityX: 0, velocityY: 0 } })
      )
    ).toBe(true);
    expect(gameService.isServing(state())).toBe(false);
    expect(gameService.winnerOf({ hostScore: 5, guestScore: 3 })).toBe('host');
    expect(gameService.winnerOf({ hostScore: 1, guestScore: 1 })).toBeNull();
  });
});

describe('snapshotBuffer', () => {
  it('returns nothing until a snapshot arrives', () => {
    expect(createSnapshotBuffer().sample(1000)).toBeNull();
  });

  it('interpolates between the snapshots around the render time', () => {
    const buffer = createSnapshotBuffer(50);

    buffer.push(
      state({ ball: { x: 0.2, y: 0.5, velocityX: 0.4, velocityY: 0 } }),
      1000
    );
    buffer.push(
      state({ ball: { x: 0.3, y: 0.5, velocityX: 0.4, velocityY: 0 } }),
      1050
    );

    // Render time 1075 - 50 = 1025 sits halfway between the two snapshots.
    expect(buffer.sample(1075)?.ballX).toBeCloseTo(0.25);
  });

  it('extrapolates briefly past the newest snapshot, then holds', () => {
    const buffer = createSnapshotBuffer(0, 100);

    buffer.push(
      state({ ball: { x: 0.5, y: 0.5, velocityX: 1, velocityY: 0 } }),
      1000
    );

    expect(buffer.sample(1050)?.ballX).toBeCloseTo(0.55);
    expect(buffer.sample(5000)?.ballX).toBeCloseTo(0.6);
  });

  it('never blends across a point being scored', () => {
    const buffer = createSnapshotBuffer(50);

    buffer.push(
      state({ ball: { x: 0.99, y: 0.5, velocityX: 1, velocityY: 0 } }),
      1000
    );
    buffer.push(
      state({ hostScore: 1, ball: { x: 0.5, y: 0.5, velocityX: 0, velocityY: 0 } }),
      1050
    );

    expect(buffer.sample(1075)?.ballX).toBeCloseTo(0.5);
  });

  it('hides the ball once the match is over', () => {
    const buffer = createSnapshotBuffer(0);

    buffer.push(state({ isRunning: false, hostScore: 5 }), 1000);

    expect(buffer.sample(1000)?.showBall).toBe(false);
  });
});

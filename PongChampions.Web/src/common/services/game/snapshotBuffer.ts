import { clamp, lerp } from '@core/utils';
import type { IBoardFrame, IGameState } from '@models/game';
import { constants } from '@shared/constants';
import { gameService } from './gameService';

interface ISnapshot {
  at: number;
  state: IGameState;
}

export interface ISnapshotBuffer {
  push: (state: IGameState, at: number) => void;
  sample: (now: number) => IBoardFrame | null;
  latest: () => IGameState | null;
  clear: () => void;
}

const capacity = 8;

// A point or a finished match teleports the ball; never blend across it.
function isDiscontinuous(previous: IGameState, next: IGameState) {
  return (
    previous.hostScore !== next.hostScore ||
    previous.guestScore !== next.guestScore ||
    previous.isRunning !== next.isRunning
  );
}

function frameOf(state: IGameState, aheadSeconds: number): IBoardFrame {
  const ball = gameService.predictBall(state.ball, aheadSeconds);

  return {
    ballX: ball.x,
    ballY: ball.y,
    hostPaddleY: state.hostPaddleY,
    guestPaddleY: state.guestPaddleY,
    showBall: state.isRunning,
  };
}

function blend(from: IGameState, to: IGameState, t: number): IBoardFrame {
  return {
    ballX: lerp(from.ball.x, to.ball.x, t),
    ballY: lerp(from.ball.y, to.ball.y, t),
    hostPaddleY: lerp(from.hostPaddleY, to.hostPaddleY, t),
    guestPaddleY: lerp(from.guestPaddleY, to.guestPaddleY, t),
    showBall: to.isRunning,
  };
}

// Server ticks arrive at 20 Hz. Rendering a little in the past keeps two snapshots
// to interpolate between, which hides network jitter and bounce corrections.
export function createSnapshotBuffer(
  delayMs: number = constants.game.interpolationDelayMs,
  maxExtrapolationMs: number = constants.game.maxExtrapolationMs
): ISnapshotBuffer {
  let _snapshots: ISnapshot[] = [];

  function push(state: IGameState, at: number) {
    const last = _snapshots[_snapshots.length - 1];

    if (last && isDiscontinuous(last.state, state)) _snapshots = [];

    _snapshots.push({ at, state });

    if (_snapshots.length > capacity) _snapshots.shift();
  }

  function sample(now: number): IBoardFrame | null {
    if (_snapshots.length === 0) return null;

    const renderAt = now - delayMs;
    let index = _snapshots.length - 1;

    while (index > 0 && _snapshots[index].at > renderAt) index--;

    const from = _snapshots[index];
    const to = _snapshots[index + 1];

    if (to && to.at > from.at)
      return blend(
        from.state,
        to.state,
        clamp((renderAt - from.at) / (to.at - from.at), 0, 1)
      );

    const ahead = clamp(renderAt - from.at, 0, maxExtrapolationMs) / 1000;

    return frameOf(from.state, ahead);
  }

  function latest() {
    return _snapshots[_snapshots.length - 1]?.state ?? null;
  }

  function clear() {
    _snapshots = [];
  }

  return { push, sample, latest, clear };
}

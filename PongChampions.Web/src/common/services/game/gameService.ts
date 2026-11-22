import { clamp, reflectInto } from '@core/utils';
import type { IBallState, IGameState, IGameSummary } from '@models/game';
import type { PlayerSide } from '@models/room';
import { constants } from '@shared/constants';

const { ballRadius, paddleHalfHeight } = constants.game;

// The server clamps paddle centers so the paddle never leaves the board.
function clampPaddleY(y: number) {
  return clamp(y, paddleHalfHeight, 1 - paddleHalfHeight);
}

// Dead-reckons the ball between server ticks, bouncing off the top and bottom walls.
function predictBall(ball: IBallState, seconds: number) {
  return {
    x: clamp(ball.x + ball.velocityX * seconds, 0, 1),
    y: reflectInto(ball.y + ball.velocityY * seconds, ballRadius, 1 - ballRadius),
  };
}

function isServing(state: IGameState) {
  return state.isRunning && state.ball.velocityX === 0 && state.ball.velocityY === 0;
}

function winnerOf(
  state: Pick<IGameState, 'hostScore' | 'guestScore'>
): PlayerSide | null {
  if (state.hostScore === state.guestScore) return null;

  return state.hostScore > state.guestScore ? 'host' : 'guest';
}

function summarize(state: IGameState): IGameSummary {
  return {
    hostScore: state.hostScore,
    guestScore: state.guestScore,
    targetScore: state.targetScore,
    isRunning: state.isRunning,
    isServing: isServing(state),
  };
}

function sameSummary(a: IGameSummary, b: IGameSummary) {
  return (
    a.hostScore === b.hostScore &&
    a.guestScore === b.guestScore &&
    a.targetScore === b.targetScore &&
    a.isRunning === b.isRunning &&
    a.isServing === b.isServing
  );
}

export const gameService = {
  clampPaddleY,
  predictBall,
  isServing,
  winnerOf,
  summarize,
  sameSummary,
};

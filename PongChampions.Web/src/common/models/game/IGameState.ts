// Board coordinates are normalized to [0, 1]; velocities are board units per second.
export interface IBallState {
  x: number;
  y: number;
  velocityX: number;
  velocityY: number;
}

export interface IGameState {
  roomCode: string;
  hostPaddleY: number;
  guestPaddleY: number;
  ball: IBallState;
  hostScore: number;
  guestScore: number;
  isRunning: boolean;
  targetScore: number;
}

// The slow-changing part of the state that the page renders (scores, phase).
export interface IGameSummary {
  hostScore: number;
  guestScore: number;
  targetScore: number;
  isRunning: boolean;
  isServing: boolean;
}

// One rendered frame of the court.
export interface IBoardFrame {
  ballX: number;
  ballY: number;
  hostPaddleY: number;
  guestPaddleY: number;
  showBall: boolean;
}

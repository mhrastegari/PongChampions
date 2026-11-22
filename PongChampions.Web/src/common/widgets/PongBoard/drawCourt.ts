import type { IBoardFrame } from '@models/game';
import { constants } from '@shared/constants';

export interface ICourtColors {
  court: string;
  line: string;
  ball: string;
  host: string;
  guest: string;
}

const { ballRadius, hostPaddleX, guestPaddleX, paddleHalfHeight } = constants.game;

const idleFrame: IBoardFrame = {
  ballX: 0.5,
  ballY: 0.5,
  hostPaddleY: 0.5,
  guestPaddleY: 0.5,
  showBall: false,
};

function fillRoundedRect(
  context: CanvasRenderingContext2D,
  x: number,
  y: number,
  width: number,
  height: number
) {
  context.beginPath();

  if (typeof context.roundRect === 'function')
    context.roundRect(x, y, width, height, width / 2);
  else context.rect(x, y, width, height);

  context.fill();
}

// Width and height are CSS pixels; the caller has already applied devicePixelRatio.
export function drawCourt(
  context: CanvasRenderingContext2D,
  width: number,
  height: number,
  frame: IBoardFrame | null,
  colors: ICourtColors
) {
  if (width === 0 || height === 0) return;

  const current = frame ?? idleFrame;

  context.fillStyle = colors.court;
  context.fillRect(0, 0, width, height);

  const dash = Math.max(6, height * 0.035);

  context.save();
  context.strokeStyle = colors.line;
  context.lineWidth = Math.max(2, width * 0.004);
  context.setLineDash([dash, dash]);
  context.beginPath();
  context.moveTo(width / 2, dash / 2);
  context.lineTo(width / 2, height);
  context.stroke();
  context.restore();

  // Physics use [0, 1] on both axes, so the ball reaches ballRadius * width sideways
  // while the drawn circle uses ballRadius * height. Shift the drawn paddle faces so
  // the circle touches the paddle exactly when the server registers a hit.
  const radius = ballRadius * height;
  const thickness = Math.max(4, width * 0.012);
  const paddleHeight = paddleHalfHeight * 2 * height;
  const hostFace = (hostPaddleX + ballRadius) * width - radius;
  const guestFace = (guestPaddleX - ballRadius) * width + radius;

  context.save();
  context.shadowBlur = Math.max(6, height * 0.03);

  context.shadowColor = colors.host;
  context.fillStyle = colors.host;
  fillRoundedRect(
    context,
    hostFace - thickness,
    current.hostPaddleY * height - paddleHeight / 2,
    thickness,
    paddleHeight
  );

  context.shadowColor = colors.guest;
  context.fillStyle = colors.guest;
  fillRoundedRect(
    context,
    guestFace,
    current.guestPaddleY * height - paddleHeight / 2,
    thickness,
    paddleHeight
  );

  if (current.showBall) {
    context.shadowColor = colors.ball;
    context.fillStyle = colors.ball;
    context.beginPath();
    context.arc(
      current.ballX * width,
      current.ballY * height,
      radius,
      0,
      Math.PI * 2
    );
    context.fill();
  }

  context.restore();
}

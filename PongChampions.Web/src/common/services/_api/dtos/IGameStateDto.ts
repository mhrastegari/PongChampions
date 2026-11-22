import type { IBallStateDto } from './IBallStateDto';
import type { IPaddleStateDto } from './IPaddleStateDto';

export interface IGameStateDto {
  roomCode: string;
  hostPaddle: IPaddleStateDto;
  guestPaddle: IPaddleStateDto;
  ball: IBallStateDto;
  hostScore: number;
  guestScore: number;
  isRunning: boolean;
  targetScore: number;
}

import type { IGameState } from '@models/game';
import { roomStatuses, type IRoom, type RoomStatus } from '@models/room';
import { constants } from '@shared/constants';
import type { IGameStateDto, IRoomDto } from '@services/_api';

export function roomStatusMapper(status: string | number): RoomStatus {
  const mapped =
    typeof status === 'number'
      ? roomStatuses[status]
      : roomStatuses.find((candidate) => candidate === status);

  if (!mapped) throw new Error(`Unknown room status: ${status}`);

  return mapped;
}

export function roomMapper(dto: IRoomDto): IRoom {
  return {
    id: dto.id,
    code: dto.code,
    createdAt: dto.createdAt,
    hostPlayerId: dto.hostPlayerId,
    hostDisplayName: dto.hostDisplayName,
    guestPlayerId: dto.guestPlayerId ?? null,
    guestDisplayName: dto.guestDisplayName ?? null,
    status: roomStatusMapper(dto.status),
    targetScore: dto.maxScore ?? constants.game.defaultTargetScore,
  };
}

export function gameStateMapper(dto: IGameStateDto): IGameState {
  return {
    roomCode: dto.roomCode,
    hostPaddleY: dto.hostPaddle.y,
    guestPaddleY: dto.guestPaddle.y,
    ball: {
      x: dto.ball.x,
      y: dto.ball.y,
      velocityX: dto.ball.velocityX,
      velocityY: dto.ball.velocityY,
    },
    hostScore: dto.hostScore,
    guestScore: dto.guestScore,
    isRunning: dto.isRunning,
    targetScore: dto.targetScore,
  };
}

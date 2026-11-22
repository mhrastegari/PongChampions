import type { IRoomDto } from './IRoomDto';

export interface IJoinRoomResponseDto {
  room: IRoomDto;
  guestToken: string | null;
}

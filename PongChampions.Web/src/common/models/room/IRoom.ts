import type { RoomStatus } from './RoomStatus';

export interface IRoom {
  id: string;
  code: string;
  createdAt: string;
  hostPlayerId: string;
  hostDisplayName: string;
  guestPlayerId: string | null;
  guestDisplayName: string | null;
  status: RoomStatus;
  targetScore: number;
}

export interface IRoomDto {
  id: string;
  createdAt: string;
  code: string;
  hostPlayerId: string;
  hostDisplayName: string;
  guestPlayerId?: string | null;
  guestDisplayName?: string | null;
  // HTTP serializes the enum name; SignalR serializes its number.
  status: string | number;
  maxScore?: number | null;
}

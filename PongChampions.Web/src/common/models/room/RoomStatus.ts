// Order matches PongChampions.Data.Enums.RoomStatus: SignalR sends the numeric value.
export const roomStatuses = [
  'WaitingForPlayers',
  'Ready',
  'InGame',
  'Finished',
  'Cancelled',
] as const;

export type RoomStatus = (typeof roomStatuses)[number];

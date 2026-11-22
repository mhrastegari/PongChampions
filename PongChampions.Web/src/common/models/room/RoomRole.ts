export type PlayerSide = 'host' | 'guest';

export type RoomRole = PlayerSide | 'spectator';

export type ConnectionStatus =
  'connecting' | 'connected' | 'reconnecting' | 'disconnected';

export type RoomAction = 'join' | 'start' | 'close' | 'leave' | 'create';

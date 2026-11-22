import {
  HubConnectionBuilder,
  LogLevel,
  type HubConnection,
  type IRetryPolicy,
} from '@microsoft/signalr';
import { http } from '@core/http';
import { constants } from '@shared/constants';
import type { IGameStateDto, IJoinedRoomDto, IRoomDto } from './dtos';

export interface IRoomHubEvents {
  JoinedRoom: (payload: IJoinedRoomDto) => void;
  LeftRoom: (code: string) => void;
  PlayerJoined: (room: IRoomDto) => void;
  PlayerLeft: (room: IRoomDto) => void;
  MatchStarted: (room: IRoomDto) => void;
  GameStateUpdated: (state: IGameStateDto) => void;
  PaddleUpdated: (state: IGameStateDto) => void;
  MatchFinished: (state: IGameStateDto) => void;
  RoomClosed: (room: IRoomDto) => void;
}

const retryDelays = [0, 1000, 2000, 4000, 8000];

// The server keeps a disconnected player's seat for 60 seconds.
const retryPolicy: IRetryPolicy = {
  nextRetryDelayInMilliseconds: ({ previousRetryCount, elapsedMilliseconds }) =>
    elapsedMilliseconds > 55_000
      ? null
      : (retryDelays[previousRetryCount] ?? 10_000),
};

// An empty token connects anonymously, which permits spectating.
function connect(accessToken: () => string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${http.getBaseUrl()}${constants.api.roomHubPath}`, {
      accessTokenFactory: accessToken,
    })
    .withAutomaticReconnect(retryPolicy)
    .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error)
    .build();
}

function on<E extends keyof IRoomHubEvents>(
  connection: HubConnection,
  event: E,
  handler: IRoomHubEvents[E]
) {
  connection.on(event, handler as (...args: unknown[]) => void);
}

function joinRoom(connection: HubConnection, code: string) {
  return connection.invoke<void>('JoinRoom', code);
}

function leaveRoom(connection: HubConnection, code: string) {
  return connection.invoke<void>('LeaveRoom', code);
}

function movePaddle(connection: HubConnection, code: string, y: number) {
  return connection.invoke<void>('MovePaddle', code, y);
}

export const roomHubApi = {
  connect,
  on,
  joinRoom,
  leaveRoom,
  movePaddle,
};

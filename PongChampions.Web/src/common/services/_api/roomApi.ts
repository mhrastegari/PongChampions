import { http } from '@core/http';
import type { IJoinRoomResponseDto, IRoomDto } from './dtos';
import { callApi } from './errors';

// undefined: the default account header; null: anonymous; string: this bearer token.
export type Credential = string | null | undefined;

function authHeaders(credential: Credential) {
  if (credential === undefined) return undefined;

  return { Authorization: credential === null ? null : `Bearer ${credential}` };
}

function roomPath(code: string, action = '') {
  return `/api/rooms/${encodeURIComponent(code)}${action ? `/${action}` : ''}`;
}

function createRoom() {
  return callApi(http.post<IRoomDto>('/api/rooms'));
}

function getRoom(code: string) {
  return callApi(http.get<IRoomDto>(roomPath(code), { headers: authHeaders(null) }));
}

function joinRoom(code: string, credential: Credential) {
  return callApi(
    http.post<IJoinRoomResponseDto>(roomPath(code, 'join'), {
      headers: authHeaders(credential),
    })
  );
}

function startMatch(code: string) {
  return callApi(http.post<IRoomDto>(roomPath(code, 'start')));
}

function closeRoom(code: string) {
  return callApi(http.post<IRoomDto>(roomPath(code, 'close')));
}

function leaveRoom(code: string, credential: Credential) {
  return callApi(
    http.post<IRoomDto>(roomPath(code, 'leave'), {
      headers: authHeaders(credential),
    })
  );
}

export const roomApi = {
  createRoom,
  getRoom,
  joinRoom,
  startMatch,
  closeRoom,
  leaveRoom,
};

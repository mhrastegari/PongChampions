import { decodeJwt } from '@core/utils';
import type { IRoom, RoomRole, RoomStatus } from '@models/room';
import { constants } from '@shared/constants';
import { roomApi } from '@services/_api';
import { authService } from '@services/auth';
import { roomCredentials } from './roomCredentials';
import { roomMapper } from './roomMapper';

const liveStatuses: RoomStatus[] = ['WaitingForPlayers', 'Ready', 'InGame'];

function normalizeCode(input: string) {
  return input.trim().toUpperCase();
}

function isValidCode(code: string) {
  return constants.room.codePattern.test(code);
}

function isLive(status: RoomStatus) {
  return liveStatuses.includes(status);
}

// A guest seat credential for this room wins over the account token.
function credentialFor(code: string) {
  return roomCredentials.getGuestToken(code) ?? authService.getToken();
}

function hubToken(code: string) {
  return credentialFor(code) ?? '';
}

function hasGuestToken(code: string) {
  return roomCredentials.getGuestToken(code) !== null;
}

// A guest token whose player no longer holds the seat (left, or replaced) is useless.
function isGuestTokenStale(room: IRoom) {
  const token = roomCredentials.getGuestToken(room.code);

  if (!token) return false;

  return decodeJwt(token)?.nameid !== room.guestPlayerId;
}

// playerId comes from the hub's JoinedRoom event; undefined means not known yet.
function roleOf(
  room: IRoom | null,
  playerId: string | null | undefined
): RoomRole | null {
  if (!room || playerId === undefined) return null;
  if (playerId !== null && playerId === room.hostPlayerId) return 'host';
  if (playerId !== null && playerId === room.guestPlayerId) return 'guest';

  return 'spectator';
}

function forgetRoom(code: string) {
  roomCredentials.removeGuestToken(code);
  roomCredentials.clearActiveRoom(code);
}

async function createRoom() {
  const room = roomMapper(await roomApi.createRoom());

  roomCredentials.setActiveRoom(room.code);

  return room;
}

async function getRoom(code: string) {
  return roomMapper(await roomApi.getRoom(code));
}

async function joinRoom(code: string) {
  // Signed in: join with the account (default header). Otherwise join as a guest.
  const result = await roomApi.joinRoom(
    code,
    authService.getToken() ? undefined : null
  );

  if (result.guestToken) roomCredentials.saveGuestToken(code, result.guestToken);

  roomCredentials.setActiveRoom(code);

  return roomMapper(result.room);
}

async function startMatch(code: string) {
  return roomMapper(await roomApi.startMatch(code));
}

async function closeRoom(code: string) {
  const room = roomMapper(await roomApi.closeRoom(code));

  forgetRoom(code);

  return room;
}

async function leaveRoom(code: string) {
  const room = roomMapper(await roomApi.leaveRoom(code, credentialFor(code)));

  forgetRoom(code);

  return room;
}

export const roomService = {
  normalizeCode,
  isValidCode,
  isLive,
  hubToken,
  hasGuestToken,
  isGuestTokenStale,
  roleOf,
  forgetRoom,
  getActiveRoom: roomCredentials.getActiveRoom,
  removeGuestToken: roomCredentials.removeGuestToken,
  createRoom,
  getRoom,
  joinRoom,
  startMatch,
  closeRoom,
  leaveRoom,
};

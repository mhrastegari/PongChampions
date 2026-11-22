import { storage } from '@core/storage';
import { isJwtExpired } from '@core/utils';
import { constants } from '@shared/constants';

// Guest JWTs are returned once, by the join call, and are the only proof of the seat.
type GuestTokens = Record<string, string>;

function readGuestTokens(): GuestTokens {
  const stored = storage.get<GuestTokens>(constants.storageKeys.guestTokens);

  if (!stored || typeof stored !== 'object') return {};

  const live = Object.fromEntries(
    Object.entries(stored).filter(
      ([, token]) => typeof token === 'string' && !isJwtExpired(token)
    )
  );

  if (Object.keys(live).length !== Object.keys(stored).length)
    storage.set(constants.storageKeys.guestTokens, live);

  return live;
}

function getGuestToken(code: string) {
  return readGuestTokens()[code] ?? null;
}

function saveGuestToken(code: string, token: string) {
  storage.set(constants.storageKeys.guestTokens, {
    ...readGuestTokens(),
    [code]: token,
  });
}

function removeGuestToken(code: string) {
  const tokens = readGuestTokens();

  if (!(code in tokens)) return;

  delete tokens[code];
  storage.set(constants.storageKeys.guestTokens, tokens);
}

// The API has no "my rooms" endpoint, so remember the room this browser is playing in.
function getActiveRoom() {
  const code = storage.get<string>(constants.storageKeys.activeRoom);

  return typeof code === 'string' && constants.room.codePattern.test(code)
    ? code
    : null;
}

function setActiveRoom(code: string) {
  storage.set(constants.storageKeys.activeRoom, code);
}

function clearActiveRoom(code: string) {
  if (getActiveRoom() === code) storage.remove(constants.storageKeys.activeRoom);
}

export const roomCredentials = {
  getGuestToken,
  saveGuestToken,
  removeGuestToken,
  getActiveRoom,
  setActiveRoom,
  clearActiveRoom,
};

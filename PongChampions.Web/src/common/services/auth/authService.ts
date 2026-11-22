import { storage } from '@core/storage';
import type { IAuthUser, IRegistration } from '@models/auth';
import { constants } from '@shared/constants';
import { apiService, authApi, UnauthorizedError } from '@services/_api';
import { authUserMapper } from './authMapper';

let _token: string | null = null;
let _user: IAuthUser | null = null;
let _expiryTimer: ReturnType<typeof setTimeout> | undefined;
const _listeners = new Set<() => void>();

function notify() {
  _listeners.forEach((listener) => listener());
}

function init() {
  apiService.onUnauthorized(logout);

  const token = storage.get<string>(constants.storageKeys.authToken);

  if (typeof token === 'string') setSession(token);

  // Keep tabs in sync when the user logs in or out elsewhere.
  window.addEventListener('storage', (event) => {
    if (event.key !== constants.storageKeys.authToken) return;

    const next = storage.get<string>(constants.storageKeys.authToken);

    if (next === _token) return;
    if (typeof next === 'string') setSession(next);
    else clearSession();
  });
}

function setSession(token: string) {
  const user = authUserMapper(token);

  if (!user || user.expiresAt <= Date.now()) {
    clearSession();

    return false;
  }

  _token = token;
  _user = user;
  storage.set(constants.storageKeys.authToken, token);
  apiService.setAuthToken(token);

  clearTimeout(_expiryTimer);
  _expiryTimer = setTimeout(logout, user.expiresAt - Date.now());

  notify();

  return true;
}

function clearSession() {
  const hadSession = _token !== null;

  clearTimeout(_expiryTimer);
  _token = null;
  _user = null;
  storage.remove(constants.storageKeys.authToken);
  apiService.setAuthToken(null);

  if (hadSession) notify();
}

async function login(username: string, password: string) {
  const { token } = await authApi.login({ username: username.trim(), password });

  if (!setSession(token)) throw new UnauthorizedError();
}

async function register(registration: IRegistration) {
  const country = registration.country?.trim();

  await authApi.register({
    username: registration.username.trim(),
    displayName: registration.displayName.trim(),
    password: registration.password,
    country: country ? country : null,
  });
  await login(registration.username, registration.password);
}

function logout() {
  clearSession();
}

function getToken() {
  return _user && _user.expiresAt > Date.now() ? _token : null;
}

function getUser() {
  return _user;
}

function subscribe(listener: () => void) {
  _listeners.add(listener);

  return () => {
    _listeners.delete(listener);
  };
}

export const authService = {
  init,
  login,
  register,
  logout,
  getToken,
  getUser,
  subscribe,
};

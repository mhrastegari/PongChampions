import { http } from '@core/http';

function init() {
  // Empty means same origin: the Vite dev proxy, or a reverse proxy in front of both.
  http.setBaseUrl(import.meta.env.VITE_API_BASE_URL ?? '');
}

// The account credential is sent by default; room calls can override it per request.
function setAuthToken(token: string | null) {
  http.setHeader('Authorization', token ? `Bearer ${token}` : null);
}

function onUnauthorized(handler: () => void) {
  http.setUnauthorizedHandler(handler);
}

export const apiService = {
  init,
  setAuthToken,
  onUnauthorized,
};

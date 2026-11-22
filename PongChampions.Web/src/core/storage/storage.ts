// localStorage can be unavailable (private mode, blocked site data); every access is guarded.

function get<T>(key: string): T | null {
  try {
    const raw = window.localStorage.getItem(key);

    return raw === null ? null : (JSON.parse(raw) as T);
  } catch {
    return null;
  }
}

function set(key: string, value: unknown) {
  try {
    window.localStorage.setItem(key, JSON.stringify(value));
  } catch {
    // Persistence is a convenience; the app keeps working in memory.
  }
}

function remove(key: string) {
  try {
    window.localStorage.removeItem(key);
  } catch {
    // See set().
  }
}

export const storage = {
  get,
  set,
  remove,
};

import { storage } from '@core/storage';
import type { ResolvedTheme, ThemePreference } from '@models/theme';
import { constants } from '@shared/constants';

let _preference: ThemePreference = 'system';
let _resolved: ResolvedTheme = 'dark';
const _listeners = new Set<() => void>();

function darkQuery() {
  return window.matchMedia('(prefers-color-scheme: dark)');
}

function isPreference(value: unknown): value is ThemePreference {
  return value === 'light' || value === 'dark' || value === 'system';
}

function init() {
  const stored = storage.get<unknown>(constants.storageKeys.theme);

  _preference = isPreference(stored) ? stored : 'system';
  darkQuery().addEventListener('change', () => {
    if (_preference === 'system') apply();
  });
  apply();
}

function apply() {
  _resolved =
    _preference === 'system'
      ? darkQuery().matches
        ? 'dark'
        : 'light'
      : _preference;
  document.documentElement.classList.toggle('dark', _resolved === 'dark');

  // Browser chrome and the safe-area strips should match the page background.
  document
    .querySelector('meta[name="theme-color"]')
    ?.setAttribute('content', cssColor('background'));

  _listeners.forEach((listener) => listener());
}

function setPreference(preference: ThemePreference) {
  _preference = preference;
  storage.set(constants.storageKeys.theme, preference);
  apply();
}

function toggle() {
  setPreference(_resolved === 'dark' ? 'light' : 'dark');
}

function getResolved() {
  return _resolved;
}

function subscribe(listener: () => void) {
  _listeners.add(listener);

  return () => {
    _listeners.delete(listener);
  };
}

// Resolves a theme token for consumers that cannot use classes, such as canvas.
function cssColor(token: string, alpha = 1) {
  const value = getComputedStyle(document.documentElement)
    .getPropertyValue(`--${token}`)
    .trim();

  return alpha === 1 ? `hsl(${value})` : `hsl(${value} / ${alpha})`;
}

export const themeService = {
  init,
  toggle,
  setPreference,
  getResolved,
  subscribe,
  cssColor,
};

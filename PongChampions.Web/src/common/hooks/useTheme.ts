import { useSyncExternalStore } from 'react';
import { themeService } from '@services/theme';

export function useTheme() {
  return useSyncExternalStore(themeService.subscribe, themeService.getResolved);
}

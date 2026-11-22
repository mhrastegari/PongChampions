import { useSyncExternalStore } from 'react';
import { authService } from '@services/auth';

export function useAuth() {
  return useSyncExternalStore(authService.subscribe, authService.getUser);
}

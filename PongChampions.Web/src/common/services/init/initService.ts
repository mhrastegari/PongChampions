import { apiService } from '@services/_api';
import { authService } from '@services/auth';
import { themeService } from '@services/theme';
import { vocabService } from '@services/vocab';

// Dependency order: transport, then presentation, then the session that uses both.
function initApp() {
  apiService.init();
  vocabService.init();
  themeService.init();
  authService.init();
}

export const initService = {
  initApp,
};

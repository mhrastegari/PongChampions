import { useEffect } from 'react';
import { vocabService } from '@services/vocab';

export function useDocumentTitle(title?: string) {
  useEffect(() => {
    const appName = vocabService.get('app_name');

    document.title = title ? `${title} · ${appName}` : appName;
  }, [title]);
}

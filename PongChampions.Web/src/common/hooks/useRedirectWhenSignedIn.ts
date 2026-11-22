import { useRouter } from '@tanstack/react-router';
import { useEffect } from 'react';
import { useAuth } from './useAuth';

// Leaves the login/register screens as soon as a session exists.
export function useRedirectWhenSignedIn(redirect: string) {
  const user = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (user) router.history.replace(redirect);
  }, [user, router, redirect]);
}

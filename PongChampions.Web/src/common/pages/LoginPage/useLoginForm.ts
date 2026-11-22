import { useState, type FormEvent } from 'react';
import { getErrorMessage, UnauthorizedError } from '@services/_api';
import { authService } from '@services/auth';
import { vocabService } from '@services/vocab';

const vocab = vocabService.get;

export function useLoginForm() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();

    if (!username.trim() || !password) {
      setError(vocab('validation_login_required'));

      return;
    }

    setPending(true);
    setError(null);

    try {
      // Success updates the session; the page redirects when it sees the user.
      await authService.login(username, password);
    } catch (failure) {
      setError(
        failure instanceof UnauthorizedError
          ? vocab('login_invalid')
          : getErrorMessage(failure)
      );
      setPending(false);
    }
  }

  return { username, setUsername, password, setPassword, error, pending, onSubmit };
}

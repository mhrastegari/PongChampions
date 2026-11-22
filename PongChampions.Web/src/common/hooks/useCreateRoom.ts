import { useNavigate } from '@tanstack/react-router';
import { useState } from 'react';
import { getErrorMessage } from '@services/_api';
import { roomService } from '@services/rooms';

export function useCreateRoom() {
  const navigate = useNavigate();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function createRoom() {
    setPending(true);
    setError(null);

    try {
      const room = await roomService.createRoom();

      await navigate({ to: '/rooms/$code', params: { code: room.code } });
    } catch (failure) {
      setError(getErrorMessage(failure));
      setPending(false);
    }
  }

  return { pending, error, createRoom };
}

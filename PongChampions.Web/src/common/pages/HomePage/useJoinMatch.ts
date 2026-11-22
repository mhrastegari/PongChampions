import { useNavigate } from '@tanstack/react-router';
import { useState, type FormEvent } from 'react';
import { constants } from '@shared/constants';
import { getErrorMessage, NotFoundError } from '@services/_api';
import { roomService } from '@services/rooms';
import { vocabService } from '@services/vocab';

const vocab = vocabService.get;

type JoinIntent = 'join' | 'watch';

export function useJoinMatch() {
  const navigate = useNavigate();
  const [code, setCodeValue] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState<JoinIntent | null>(null);

  function setCode(value: string) {
    setCodeValue(
      value
        .toUpperCase()
        .replace(/[^A-Z0-9]/g, '')
        .slice(0, constants.room.codeLength)
    );
    setError(null);
  }

  function openRoom(roomCode: string) {
    return navigate({ to: '/rooms/$code', params: { code: roomCode } });
  }

  async function submit(intent: JoinIntent) {
    if (!roomService.isValidCode(code)) {
      setError(vocab('validation_room_code'));

      return;
    }

    // Watching, or returning to a seat this browser already holds, needs no join call.
    if (
      intent === 'watch' ||
      roomService.hasGuestToken(code) ||
      roomService.getActiveRoom() === code
    ) {
      await openRoom(code);

      return;
    }

    setPending(intent);
    setError(null);

    try {
      await roomService.joinRoom(code);
      await openRoom(code);
    } catch (failure) {
      setError(
        failure instanceof NotFoundError
          ? vocab('room_not_found')
          : getErrorMessage(failure)
      );
      setPending(null);
    }
  }

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    void submit('join');
  }

  return {
    code,
    setCode,
    error,
    pending,
    onSubmit,
    watch: () => submit('watch'),
  };
}

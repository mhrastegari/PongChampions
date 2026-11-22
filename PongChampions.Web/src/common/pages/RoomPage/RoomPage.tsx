import { useParams } from '@tanstack/react-router';
import { NotFoundPage } from '@pages/NotFoundPage';
import { roomService } from '@services/rooms';
import { vocabService } from '@services/vocab';
import { RoomScreen } from './RoomScreen';

const vocab = vocabService.get;

export function RoomPage() {
  const params = useParams({ from: '/rooms/$code' });
  const code = roomService.normalizeCode(params.code);

  if (!roomService.isValidCode(code))
    return (
      <NotFoundPage
        title={vocab('room_not_found')}
        description={vocab('room_invalid_code')}
      />
    );

  // A new key gives each room a fresh connection and state.
  return <RoomScreen key={code} code={code} />;
}

import { useDocumentTitle } from '@hooks/useDocumentTitle';
import { constants } from '@shared/constants';
import { vocabService } from '@services/vocab';
import { NotFoundPage } from '@pages/NotFoundPage';
import { PongBoard } from '@widgets/PongBoard';
import { Scoreboard } from '@widgets/Scoreboard';
import { RoomHeader } from './RoomHeader';
import { RoomOverlay } from './RoomOverlay';
import { RoomPanel } from './RoomPanel';
import { useRoomSession } from './useRoomSession';

const { format, get: vocab } = vocabService;

export function RoomScreen({ code }: { code: string }) {
  const session = useRoomSession(code);
  const { room, summary } = session;

  useDocumentTitle(format('room_title', code));

  if (session.notFound)
    return (
      <NotFoundPage
        title={vocab('room_not_found')}
        description={vocab('room_not_found_description')}
      />
    );

  // A finished room loaded later has no live state, so its scores are unknown.
  const scoresKnown = summary !== null || room?.status !== 'Finished';

  return (
    <div className="flex flex-col gap-4 sm:gap-6">
      <RoomHeader code={code} room={room} connection={session.connection} />
      <Scoreboard
        hostName={room?.hostDisplayName ?? null}
        guestName={room?.guestDisplayName ?? null}
        hostScore={scoresKnown ? (summary?.hostScore ?? 0) : null}
        guestScore={scoresKnown ? (summary?.guestScore ?? 0) : null}
        targetScore={
          summary?.targetScore ??
          room?.targetScore ??
          constants.game.defaultTargetScore
        }
        viewerSide={session.viewerSide}
      />
      <PongBoard
        buffer={session.buffer}
        localSide={session.viewerSide}
        controlsEnabled={session.canControl}
        onPaddleMove={session.movePaddle}
        label={vocab('board_label')}
      >
        <RoomOverlay session={session} />
      </PongBoard>
      <RoomPanel session={session} />
    </div>
  );
}

import { Spinner } from '@components/Spinner';
import { gameService } from '@services/game';
import { vocabService } from '@services/vocab';
import type { RoomSession } from './useRoomSession';

const { format, get: vocab } = vocabService;

interface IOverlayMessage {
  title: string;
  detail?: string;
  busy?: boolean;
}

function messageFor(session: RoomSession): IOverlayMessage | null {
  const { room, role, summary, connection, viewerSide } = session;

  if (connection === 'reconnecting')
    return {
      title: vocab('overlay_reconnecting'),
      detail: vocab('overlay_reconnecting_detail'),
      busy: true,
    };

  if (!room) return { title: vocab('overlay_loading'), busy: true };

  switch (room.status) {
    case 'WaitingForPlayers':
      return {
        title: vocab('overlay_waiting_title'),
        detail: vocab(
          role === 'host' ? 'overlay_waiting_host' : 'overlay_waiting_visitor'
        ),
      };
    case 'Ready':
      if (role === 'host') return { title: vocab('overlay_ready_host') };

      return {
        title:
          role === 'guest'
            ? format('overlay_ready_guest', room.hostDisplayName)
            : vocab('overlay_ready_spectator'),
      };
    case 'InGame':
      if (connection === 'disconnected')
        return { title: vocab('overlay_disconnected') };

      return !summary || summary.isServing
        ? { title: vocab('overlay_get_ready') }
        : null;
    case 'Finished': {
      if (!summary) return { title: vocab('overlay_finished') };

      const winner = gameService.winnerOf(summary);
      const detail = format('final_score', summary.hostScore, summary.guestScore);

      if (viewerSide && winner)
        return {
          title: vocab(
            viewerSide === winner ? 'overlay_you_win' : 'overlay_you_lose'
          ),
          detail,
        };

      const name = winner === 'guest' ? room.guestDisplayName : room.hostDisplayName;

      return {
        title: format('overlay_winner', name ?? vocab('side_guest')),
        detail,
      };
    }
    case 'Cancelled':
      return {
        title: vocab('overlay_cancelled_title'),
        detail: vocab('overlay_cancelled_detail'),
      };
  }
}

export function RoomOverlay({ session }: { session: RoomSession }) {
  const message = messageFor(session);

  if (!message) return null;

  return (
    <div
      role="status"
      className="flex max-w-sm flex-col items-center gap-1 rounded-xl bg-court/75 px-4 py-3 text-center text-court-foreground backdrop-blur-sm sm:gap-2 sm:px-6 sm:py-4"
    >
      <p className="flex items-center gap-2 text-base font-bold sm:text-2xl">
        {message.busy && <Spinner className="size-4 sm:size-5" />}
        {message.title}
      </p>
      {message.detail && (
        <p className="text-xs text-court-foreground/75 sm:text-sm">
          {message.detail}
        </p>
      )}
    </div>
  );
}

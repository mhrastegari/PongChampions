import { Badge, type BadgeTone } from '@components/Badge';
import { CopyButton } from '@components/CopyButton';
import type { ConnectionStatus, IRoom, RoomStatus } from '@models/room';
import { vocabService } from '@services/vocab';

const vocab = vocabService.get;

const statusBadges = {
  WaitingForPlayers: { tone: 'warning', label: 'status_waiting' },
  Ready: { tone: 'primary', label: 'status_ready' },
  InGame: { tone: 'success', label: 'status_in_game' },
  Finished: { tone: 'neutral', label: 'status_finished' },
  Cancelled: { tone: 'danger', label: 'status_cancelled' },
} satisfies Record<RoomStatus, { tone: BadgeTone; label: string }>;

const connectionBadges = {
  connecting: { tone: 'neutral', label: 'connection_connecting' },
  connected: { tone: 'success', label: 'connection_connected' },
  reconnecting: { tone: 'warning', label: 'connection_reconnecting' },
  disconnected: { tone: 'danger', label: 'connection_disconnected' },
} satisfies Record<ConnectionStatus, { tone: BadgeTone; label: string }>;

interface IRoomHeaderProps {
  code: string;
  room: IRoom | null;
  connection: ConnectionStatus;
}

export function RoomHeader({ code, room, connection }: IRoomHeaderProps) {
  const status = room ? statusBadges[room.status] : null;
  const live = connectionBadges[connection];

  return (
    <header className="flex flex-wrap items-center justify-between gap-3">
      <div className="flex min-w-0 flex-col gap-1">
        <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">
          {vocab('room_label')}
        </p>
        <h1 className="font-mono text-3xl font-black tracking-[0.2em] sm:text-4xl">
          {code}
        </h1>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        {status && <Badge tone={status.tone}>{vocab(status.label)}</Badge>}
        <Badge tone={live.tone}>
          <span
            aria-hidden="true"
            className="size-1.5 rounded-full bg-current motion-safe:animate-pulse"
          />
          {vocab(live.label)}
        </Badge>
        <CopyButton
          value={window.location.href}
          label={vocab('copy_link')}
          copiedLabel={vocab('copied')}
          failedLabel={vocab('copy_failed')}
        />
      </div>
    </header>
  );
}

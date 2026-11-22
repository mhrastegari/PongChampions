import { Link, useNavigate } from '@tanstack/react-router';
import { Alert } from '@components/Alert';
import { Button, buttonClassName } from '@components/Button';
import { ConfirmButton } from '@components/ConfirmButton';
import { CopyButton } from '@components/CopyButton';
import { useAuth } from '@hooks/useAuth';
import { useCreateRoom } from '@hooks/useCreateRoom';
import { roomService } from '@services/rooms';
import { vocabService } from '@services/vocab';
import type { RoomSession } from './useRoomSession';

const { format, get: vocab } = vocabService;

const roleLabels = {
  host: 'role_host',
  guest: 'role_guest',
  spectator: 'role_spectator',
} as const;

export function RoomPanel({ session }: { session: RoomSession }) {
  const user = useAuth();
  const navigate = useNavigate();
  const newMatch = useCreateRoom();
  const { room, role, connection, pendingAction, error, actions } = session;

  if (!room) return error ? <Alert>{error}</Alert> : null;

  const live = roomService.isLive(room.status);
  const busy = pendingAction !== null;

  async function leave() {
    if (await actions.leave()) await navigate({ to: '/' });
  }

  return (
    <section
      aria-label={vocab('room_controls')}
      className="flex flex-col gap-4 rounded-2xl border border-border bg-card p-4 sm:p-6"
    >
      {error && <Alert>{error}</Alert>}
      {newMatch.error && <Alert>{newMatch.error}</Alert>}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-muted-foreground">
          {/* Ended rooms refuse hub joins, so the role may never resolve there. */}
          {role ? vocab(roleLabels[role]) : live ? vocab('role_unknown') : ''}
        </p>

        <div className="flex flex-wrap items-center gap-2">
          {connection === 'disconnected' && live && (
            <Button variant="secondary" onClick={actions.reconnect}>
              {vocab('reconnect')}
            </Button>
          )}

          {role === 'host' && room.status === 'WaitingForPlayers' && (
            <CopyButton
              value={room.code}
              label={vocab('copy_code')}
              copiedLabel={vocab('copied')}
              failedLabel={vocab('copy_failed')}
            />
          )}

          {session.canJoin && (
            <Button
              size="lg"
              loading={pendingAction === 'join'}
              disabled={busy}
              onClick={actions.join}
            >
              {user ? format('join_as', user.username) : vocab('join_as_guest')}
            </Button>
          )}

          {role === 'host' && room.status === 'Ready' && (
            <Button
              size="lg"
              loading={pendingAction === 'start'}
              disabled={busy || connection !== 'connected'}
              onClick={actions.start}
            >
              {vocab('start_match')}
            </Button>
          )}

          {!live && (
            <>
              <Link to="/" className={buttonClassName({ variant: 'secondary' })}>
                {vocab('back_home')}
              </Link>
              {user && (
                <Button loading={newMatch.pending} onClick={newMatch.createRoom}>
                  {vocab('host_new_match')}
                </Button>
              )}
            </>
          )}
        </div>
      </div>

      {session.canJoin && !user && (
        <p className="text-sm text-muted-foreground">
          {vocab('join_account_prompt')}{' '}
          <Link
            to="/login"
            search={{ redirect: `/rooms/${room.code}` }}
            className="font-semibold text-primary underline-offset-4 hover:underline"
          >
            {vocab('login')}
          </Link>
        </p>
      )}

      {session.viewerSide && room.status === 'InGame' && (
        <p className="text-sm text-muted-foreground">{vocab('controls_hint')}</p>
      )}

      {live && (role === 'host' || role === 'guest') && (
        <div className="flex flex-wrap justify-end border-t border-border pt-4">
          {role === 'host' ? (
            <ConfirmButton
              label={vocab('room_close')}
              prompt={vocab('room_close_prompt')}
              confirmLabel={vocab('room_close_confirm')}
              cancelLabel={vocab('cancel')}
              loading={pendingAction === 'close'}
              disabled={busy && pendingAction !== 'close'}
              onConfirm={actions.close}
            />
          ) : (
            <ConfirmButton
              label={vocab('room_leave')}
              prompt={vocab(
                room.status === 'InGame'
                  ? 'room_leave_prompt_in_game'
                  : 'room_leave_prompt'
              )}
              confirmLabel={vocab('room_leave_confirm')}
              cancelLabel={vocab('cancel')}
              loading={pendingAction === 'leave'}
              disabled={busy && pendingAction !== 'leave'}
              onConfirm={leave}
            />
          )}
        </div>
      )}
    </section>
  );
}

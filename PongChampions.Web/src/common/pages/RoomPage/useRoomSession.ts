import type { HubConnection } from '@microsoft/signalr';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useAuth } from '@hooks/useAuth';
import type { IGameSummary } from '@models/game';
import type { ConnectionStatus, IRoom, PlayerSide, RoomAction } from '@models/room';
import { constants } from '@shared/constants';
import {
  getErrorMessage,
  NotFoundError,
  roomHubApi,
  type IGameStateDto,
  type IRoomDto,
} from '@services/_api';
import { createSnapshotBuffer, gameService } from '@services/game';
import { gameStateMapper, roomMapper, roomService } from '@services/rooms';
import { vocabService } from '@services/vocab';

const vocab = vocabService.get;

interface IPaddleOutbox {
  target: number | null;
  sent: number | null;
  inFlight: boolean;
}

// Owns one room's live connection: HTTP snapshot, SignalR events, paddle input and
// lifecycle actions. High-frequency game state goes to the snapshot buffer, not React.
export function useRoomSession(code: string) {
  const user = useAuth();
  const userId = user?.userId ?? null;
  const [buffer] = useState(createSnapshotBuffer);
  const [room, setRoom] = useState<IRoom | null>(null);
  const [notFound, setNotFound] = useState(false);
  // undefined until the hub confirms who this connection is; null for spectators.
  const [playerId, setPlayerId] = useState<string | null | undefined>(undefined);
  const [connection, setConnection] = useState<ConnectionStatus>('connecting');
  const [summary, setSummary] = useState<IGameSummary | null>(null);
  const [pendingAction, setPendingAction] = useState<RoomAction | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [connectionKey, setConnectionKey] = useState(0);

  const hubRef = useRef<HubConnection | null>(null);
  const roomEvents = useRef(0);
  const outbox = useRef<IPaddleOutbox>({
    target: null,
    sent: null,
    inFlight: false,
  });

  const refreshRoom = useCallback(async () => {
    const eventsBefore = roomEvents.current;

    try {
      const next = await roomService.getRoom(code);

      // A hub event that arrived meanwhile is newer than this response.
      if (roomEvents.current === eventsBefore) setRoom(next);
      if (!roomService.isLive(next.status)) roomService.forgetRoom(code);

      return next;
    } catch (failure) {
      if (failure instanceof NotFoundError) {
        setNotFound(true);
        roomService.forgetRoom(code);
      } else setError(getErrorMessage(failure));

      return null;
    }
  }, [code]);

  useEffect(() => {
    void refreshRoom();
  }, [refreshRoom]);

  useEffect(() => {
    let disposed = false;
    const hub = roomHubApi.connect(() => roomService.hubToken(code));

    hubRef.current = hub;
    setPlayerId(undefined);
    setConnection('connecting');

    const applyRoom = (dto: IRoomDto) => {
      roomEvents.current++;
      setRoom(roomMapper(dto));
    };

    const applyState = (dto: IGameStateDto) => {
      const state = gameStateMapper(dto);
      const next = gameService.summarize(state);

      buffer.push(state, performance.now());
      setSummary((previous) =>
        previous && gameService.sameSummary(previous, next) ? previous : next
      );
    };

    roomHubApi.on(hub, 'JoinedRoom', (payload) => setPlayerId(payload.playerId));
    roomHubApi.on(hub, 'PlayerJoined', applyRoom);
    roomHubApi.on(hub, 'PlayerLeft', applyRoom);
    roomHubApi.on(hub, 'MatchStarted', (dto) => {
      buffer.clear();
      setSummary(null);
      applyRoom(dto);
    });
    roomHubApi.on(hub, 'GameStateUpdated', applyState);
    roomHubApi.on(hub, 'MatchFinished', (dto) => {
      applyState(dto);
      roomEvents.current++;
      setRoom((previous) => previous && { ...previous, status: 'Finished' });
      roomService.forgetRoom(code);
    });
    roomHubApi.on(hub, 'RoomClosed', (dto) => {
      applyRoom(dto);
      roomService.forgetRoom(code);
    });

    const join = async () => {
      try {
        await roomHubApi.joinRoom(hub, code);
      } catch (failure) {
        if (disposed) return;

        const latest = await refreshRoom();

        if (disposed) return;

        // The seat this browser held is gone: reconnect with the next credential.
        if (latest && roomService.isGuestTokenStale(latest)) {
          roomService.removeGuestToken(code);
          setConnectionKey((key) => key + 1);

          return;
        }

        setPlayerId(null);

        if (latest && roomService.isLive(latest.status))
          setError(getErrorMessage(failure));
      }
    };

    hub.onreconnecting(() => {
      if (!disposed) setConnection('reconnecting');
    });
    hub.onreconnected(() => {
      if (disposed) return;

      setConnection('connected');
      void join();
    });
    hub.onclose(() => {
      if (!disposed) setConnection('disconnected');
    });

    // Deferred one task: React StrictMode's immediate unmount then cancels it
    // cleanly instead of aborting a negotiation already on the wire.
    const startTimer = window.setTimeout(() => {
      hub
        .start()
        .then(() => {
          if (disposed) return;

          setConnection('connected');

          return join();
        })
        .catch(() => {
          if (disposed) return;

          setConnection('disconnected');
          setError(vocab('error_connection'));
        });
    });

    return () => {
      disposed = true;
      window.clearTimeout(startTimer);
      hubRef.current = null;
      void hub.stop();
    };
  }, [code, connectionKey, userId, buffer, refreshRoom]);

  const role = roomService.roleOf(room, playerId);
  const viewerSide: PlayerSide | null =
    role === 'host' || role === 'guest' ? role : null;
  const canControl =
    viewerSide !== null &&
    room?.status === 'InGame' &&
    connection === 'connected' &&
    summary?.isRunning !== false;
  const canJoin =
    role === 'spectator' &&
    room?.status === 'WaitingForPlayers' &&
    room.guestPlayerId === null &&
    connection === 'connected';

  // Sends the newest paddle position at a fixed rate, one call in flight at a time.
  useEffect(() => {
    outbox.current = { target: null, sent: null, inFlight: false };

    if (!canControl) return;

    const timer = window.setInterval(() => {
      const pending = outbox.current;
      const hub = hubRef.current;

      if (!hub || pending.inFlight || pending.target === null) return;
      if (pending.target === pending.sent) return;

      const y = pending.target;

      pending.inFlight = true;
      roomHubApi
        .movePaddle(hub, code, y)
        .then(() => {
          pending.sent = y;
        })
        .catch(() => {
          // Retried on the next tick; persistent failures end with the match.
        })
        .finally(() => {
          pending.inFlight = false;
        });
    }, constants.game.paddleSendIntervalMs);

    return () => window.clearInterval(timer);
  }, [canControl, code]);

  const movePaddle = useCallback((y: number) => {
    outbox.current.target = y;
  }, []);

  async function run(action: RoomAction, task: () => Promise<void>) {
    setPendingAction(action);
    setError(null);

    try {
      await task();

      return true;
    } catch (failure) {
      setError(getErrorMessage(failure));

      return false;
    } finally {
      setPendingAction(null);
    }
  }

  const actions = {
    join: () =>
      run('join', async () => {
        roomEvents.current++;
        setRoom(await roomService.joinRoom(code));
        // The seat is bound when JoinRoom runs, so register again with the new credential.
        setConnectionKey((key) => key + 1);
      }),
    start: () =>
      run('start', async () => {
        const next = await roomService.startMatch(code);

        roomEvents.current++;
        setRoom((previous) => (previous?.status === 'InGame' ? previous : next));
      }),
    close: () =>
      run('close', async () => {
        roomEvents.current++;
        setRoom(await roomService.closeRoom(code));
      }),
    leave: () =>
      run('leave', async () => {
        await roomService.leaveRoom(code);
      }),
    reconnect: () => {
      setError(null);
      setConnectionKey((key) => key + 1);
    },
  };

  return {
    room,
    notFound,
    role,
    viewerSide,
    connection,
    summary,
    buffer,
    error,
    pendingAction,
    canControl,
    canJoin,
    movePaddle,
    actions,
  };
}

export type RoomSession = ReturnType<typeof useRoomSession>;

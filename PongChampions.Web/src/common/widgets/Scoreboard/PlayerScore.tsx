import clsx from 'clsx';
import { Badge } from '@components/Badge';
import type { PlayerSide } from '@models/room';
import { vocabService } from '@services/vocab';

const vocab = vocabService.get;

interface IPlayerScoreProps {
  side: PlayerSide;
  name: string | null;
  score: number | null;
  isViewer: boolean;
}

export function PlayerScore({ side, name, score, isViewer }: IPlayerScoreProps) {
  const isGuest = side === 'guest';

  return (
    <div
      className={clsx('flex min-w-0 items-center gap-3 sm:gap-4', {
        'flex-row-reverse text-end': isGuest,
      })}
    >
      <span
        className={clsx(
          'font-mono text-4xl font-black tabular-nums sm:text-5xl',
          isGuest ? 'text-guest' : 'text-host'
        )}
      >
        {score ?? '–'}
      </span>
      <div className={clsx('flex min-w-0 flex-col gap-1', { 'items-end': isGuest })}>
        <span
          className={clsx('flex min-w-0 items-center gap-2', {
            'flex-row-reverse': isGuest,
          })}
        >
          <span
            aria-hidden="true"
            className={clsx(
              'size-2.5 shrink-0 rounded-full',
              isGuest ? 'bg-guest' : 'bg-host'
            )}
          />
          <span
            className={clsx('truncate font-semibold', {
              'text-muted-foreground': !name,
            })}
            title={name ?? undefined}
          >
            {name ?? vocab('scoreboard_open_seat')}
          </span>
        </span>
        <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
          {vocab(isGuest ? 'side_guest' : 'side_host')}
          {isViewer && <Badge tone="primary">{vocab('you')}</Badge>}
        </span>
      </div>
    </div>
  );
}

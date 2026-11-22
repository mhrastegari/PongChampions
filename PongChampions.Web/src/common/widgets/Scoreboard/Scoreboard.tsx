import type { PlayerSide } from '@models/room';
import { vocabService } from '@services/vocab';
import { PlayerScore } from './PlayerScore';

const { format, get: vocab } = vocabService;

interface IScoreboardProps {
  hostName: string | null;
  guestName: string | null;
  // null when the scores are not known, e.g. a finished room loaded later.
  hostScore: number | null;
  guestScore: number | null;
  targetScore: number;
  viewerSide: PlayerSide | null;
}

export function Scoreboard({
  hostName,
  guestName,
  hostScore,
  guestScore,
  targetScore,
  viewerSide,
}: IScoreboardProps) {
  return (
    <div className="grid grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-2 rounded-2xl border border-border bg-card px-3 py-3 sm:gap-6 sm:px-6 sm:py-4">
      <PlayerScore
        side="host"
        name={hostName}
        score={hostScore}
        isViewer={viewerSide === 'host'}
      />
      <div className="flex flex-col items-center text-center">
        <span className="text-[0.65rem] font-semibold uppercase tracking-widest text-muted-foreground sm:text-xs">
          {vocab('first_to')}
        </span>
        <span className="font-mono text-lg font-bold tabular-nums sm:text-2xl">
          {targetScore}
        </span>
      </div>
      <PlayerScore
        side="guest"
        name={guestName}
        score={guestScore}
        isViewer={viewerSide === 'guest'}
      />
      {hostScore !== null && guestScore !== null && (
        <p className="sr-only" aria-live="polite">
          {format(
            'score_announcement',
            hostName ?? vocab('side_host'),
            hostScore,
            guestName ?? vocab('side_guest'),
            guestScore
          )}
        </p>
      )}
    </div>
  );
}

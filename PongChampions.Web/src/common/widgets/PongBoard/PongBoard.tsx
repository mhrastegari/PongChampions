import clsx from 'clsx';
import { useRef, type ReactNode } from 'react';
import type { PlayerSide } from '@models/room';
import type { ISnapshotBuffer } from '@services/game';
import { usePaddleInput } from './usePaddleInput';
import { usePongRenderer } from './usePongRenderer';

interface IPongBoardProps {
  buffer: ISnapshotBuffer;
  // The side the viewer plays, or null for spectators.
  localSide: PlayerSide | null;
  controlsEnabled: boolean;
  onPaddleMove: (y: number) => void;
  label: string;
  children?: ReactNode;
}

export function PongBoard({
  buffer,
  localSide,
  controlsEnabled,
  onPaddleMove,
  label,
  children,
}: IPongBoardProps) {
  const surfaceRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const controlling = controlsEnabled && localSide !== null;

  const localPaddleY = usePaddleInput(
    surfaceRef,
    controlling,
    () => {
      const state = buffer.latest();

      if (!state) return null;

      return localSide === 'guest' ? state.guestPaddleY : state.hostPaddleY;
    },
    onPaddleMove
  );

  usePongRenderer(canvasRef, buffer, controlling ? localSide : null, localPaddleY);

  return (
    <div
      ref={surfaceRef}
      className={clsx(
        'relative aspect-court w-full select-none overflow-hidden rounded-2xl border border-border bg-court shadow-lg',
        {
          // Touch drags move the paddle instead of scrolling the page.
          'cursor-row-resize touch-none': controlling,
        }
      )}
    >
      <canvas
        ref={canvasRef}
        className="absolute inset-0 size-full"
        role="img"
        aria-label={label}
      />
      {children && (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center p-3 sm:p-6">
          {children}
        </div>
      )}
    </div>
  );
}

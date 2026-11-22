import { useEffect, type MutableRefObject, type RefObject } from 'react';
import { useTheme } from '@hooks/useTheme';
import type { IBoardFrame } from '@models/game';
import type { PlayerSide } from '@models/room';
import type { ISnapshotBuffer } from '@services/game';
import { themeService } from '@services/theme';
import { drawCourt, type ICourtColors } from './drawCourt';

const { cssColor } = themeService;

function withLocalPaddle(
  frame: IBoardFrame,
  side: PlayerSide,
  y: number
): IBoardFrame {
  return side === 'host'
    ? { ...frame, hostPaddleY: y }
    : { ...frame, guestPaddleY: y };
}

// Draws every animation frame. The viewer's own paddle follows local input
// immediately instead of waiting a round trip for the server to echo it.
export function usePongRenderer(
  canvasRef: RefObject<HTMLCanvasElement>,
  buffer: ISnapshotBuffer,
  localSide: PlayerSide | null,
  localPaddleY: MutableRefObject<number | null>
) {
  const theme = useTheme();

  useEffect(() => {
    const canvas = canvasRef.current;
    const context = canvas?.getContext('2d');

    if (!canvas || !context) return;

    const colors: ICourtColors = {
      court: cssColor('court'),
      line: cssColor('court-foreground', 0.18),
      ball: cssColor('court-foreground'),
      host: cssColor('host'),
      guest: cssColor('guest'),
    };
    let width = 0;
    let height = 0;
    let frameId = 0;

    const resize = () => {
      const rect = canvas.getBoundingClientRect();
      const ratio = window.devicePixelRatio || 1;

      width = rect.width;
      height = rect.height;
      canvas.width = Math.round(width * ratio);
      canvas.height = Math.round(height * ratio);
      context.setTransform(ratio, 0, 0, ratio, 0, 0);
    };

    const render = (time: number) => {
      const frame = buffer.sample(time);
      const localY = localPaddleY.current;

      drawCourt(
        context,
        width,
        height,
        frame && localSide && localY !== null
          ? withLocalPaddle(frame, localSide, localY)
          : frame,
        colors
      );
      frameId = requestAnimationFrame(render);
    };

    const observer = new ResizeObserver(resize);

    observer.observe(canvas);
    resize();
    frameId = requestAnimationFrame(render);

    return () => {
      cancelAnimationFrame(frameId);
      observer.disconnect();
    };
  }, [canvasRef, buffer, localSide, localPaddleY, theme]);
}

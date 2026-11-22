import { useEffect, useRef, type RefObject } from 'react';
import { gameService } from '@services/game';
import { constants } from '@shared/constants';

type Direction = 'up' | 'down';

function directionOf(event: KeyboardEvent): Direction | null {
  switch (event.key) {
    case 'ArrowUp':
    case 'w':
    case 'W':
      return 'up';
    case 'ArrowDown':
    case 's':
    case 'S':
      return 'down';
    default:
      return null;
  }
}

function isTypingTarget(target: EventTarget | null) {
  return (
    target instanceof HTMLElement &&
    (target.isContentEditable ||
      ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName))
  );
}

// Pointer (mouse, touch, pen) over the court sets the paddle center directly;
// arrow keys or W/S move it at a fixed speed. Returns the locally predicted Y.
export function usePaddleInput(
  surfaceRef: RefObject<HTMLElement>,
  enabled: boolean,
  readServerY: () => number | null,
  onMove: (y: number) => void
) {
  const localY = useRef<number | null>(null);
  const onMoveRef = useRef(onMove);
  const readServerYRef = useRef(readServerY);

  useEffect(() => {
    onMoveRef.current = onMove;
    readServerYRef.current = readServerY;
  });

  useEffect(() => {
    const surface = surfaceRef.current;

    if (!enabled || !surface) {
      localY.current = null;

      return;
    }

    const update = (y: number) => {
      const next = gameService.clampPaddleY(y);

      if (next === localY.current) return;

      localY.current = next;
      onMoveRef.current(next);
    };

    const onPointer = (event: PointerEvent) => {
      const rect = surface.getBoundingClientRect();

      if (rect.height > 0) update((event.clientY - rect.top) / rect.height);
    };

    const pressed = new Set<Direction>();
    let frameId = 0;
    let lastTime: number | null = null;

    const step = (time: number) => {
      const direction = (pressed.has('down') ? 1 : 0) - (pressed.has('up') ? 1 : 0);

      if (lastTime !== null && direction !== 0) {
        const from = localY.current ?? readServerYRef.current() ?? 0.5;
        const seconds = Math.min(time - lastTime, 100) / 1000;

        update(from + direction * constants.game.keyboardPaddleSpeed * seconds);
      }

      if (pressed.size > 0) {
        lastTime = time;
        frameId = requestAnimationFrame(step);
      } else {
        lastTime = null;
        frameId = 0;
      }
    };

    const onKeyDown = (event: KeyboardEvent) => {
      const direction = directionOf(event);

      if (
        !direction ||
        isTypingTarget(event.target) ||
        event.altKey ||
        event.ctrlKey ||
        event.metaKey
      )
        return;

      event.preventDefault();
      pressed.add(direction);

      if (frameId === 0) frameId = requestAnimationFrame(step);
    };

    const onKeyUp = (event: KeyboardEvent) => {
      const direction = directionOf(event);

      if (direction) pressed.delete(direction);
    };

    const onBlur = () => pressed.clear();

    surface.addEventListener('pointermove', onPointer);
    surface.addEventListener('pointerdown', onPointer);
    window.addEventListener('keydown', onKeyDown);
    window.addEventListener('keyup', onKeyUp);
    window.addEventListener('blur', onBlur);

    return () => {
      surface.removeEventListener('pointermove', onPointer);
      surface.removeEventListener('pointerdown', onPointer);
      window.removeEventListener('keydown', onKeyDown);
      window.removeEventListener('keyup', onKeyUp);
      window.removeEventListener('blur', onBlur);
      cancelAnimationFrame(frameId);
      localY.current = null;
    };
  }, [enabled, surfaceRef]);

  return localY;
}

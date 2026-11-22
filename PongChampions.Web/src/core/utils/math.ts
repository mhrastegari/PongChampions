export function clamp(value: number, min: number, max: number) {
  return Math.min(max, Math.max(min, value));
}

export function lerp(from: number, to: number, t: number) {
  return from + (to - from) * t;
}

// Folds a position into [min, max] as if it bounced off both ends.
export function reflectInto(value: number, min: number, max: number) {
  const span = max - min;

  if (span <= 0) return min;

  const period = span * 2;
  const offset = (((value - min) % period) + period) % period;

  return offset <= span ? min + offset : max - (offset - span);
}

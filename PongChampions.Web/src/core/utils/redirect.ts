// Only same-origin paths are allowed as post-login destinations (no open redirects).
export function safeRedirect(value: unknown): string | undefined {
  if (typeof value !== 'string') return undefined;
  if (!value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\'))
    return undefined;

  return value;
}

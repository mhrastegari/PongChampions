import { useEffect, useState } from 'react';
import { Button, type ButtonVariant } from '@components/Button';

interface ICopyButtonProps {
  value: string;
  label: string;
  copiedLabel: string;
  failedLabel: string;
  variant?: ButtonVariant;
}

type CopyState = 'idle' | 'copied' | 'failed';

export function CopyButton({
  value,
  label,
  copiedLabel,
  failedLabel,
  variant = 'secondary',
}: ICopyButtonProps) {
  const [state, setState] = useState<CopyState>('idle');

  useEffect(() => {
    if (state === 'idle') return;

    const timer = window.setTimeout(() => setState('idle'), 2000);

    return () => window.clearTimeout(timer);
  }, [state]);

  async function copy() {
    try {
      await navigator.clipboard.writeText(value);
      setState('copied');
    } catch {
      setState('failed');
    }
  }

  return (
    <Button variant={variant} onClick={copy} aria-live="polite">
      {state === 'copied' ? copiedLabel : state === 'failed' ? failedLabel : label}
    </Button>
  );
}

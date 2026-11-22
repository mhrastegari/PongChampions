import clsx from 'clsx';
import type { ReactNode } from 'react';

type AlertTone = 'error' | 'info';

interface IAlertProps {
  tone?: AlertTone;
  children: ReactNode;
  className?: string;
}

export function Alert({ tone = 'error', children, className }: IAlertProps) {
  return (
    <div
      role={tone === 'error' ? 'alert' : 'status'}
      className={clsx(
        'break-words rounded-lg border px-4 py-3 text-sm',
        {
          'border-destructive/40 bg-destructive/10 text-destructive':
            tone === 'error',
          'border-border bg-muted text-foreground': tone === 'info',
        },
        className
      )}
    >
      {children}
    </div>
  );
}

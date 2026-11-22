import clsx from 'clsx';
import type { ReactNode } from 'react';

export type BadgeTone = 'neutral' | 'primary' | 'success' | 'warning' | 'danger';

interface IBadgeProps {
  tone?: BadgeTone;
  children: ReactNode;
  className?: string;
}

export function Badge({ tone = 'neutral', children, className }: IBadgeProps) {
  return (
    <span
      className={clsx(
        'inline-flex items-center gap-1.5 whitespace-nowrap rounded-full border px-2.5 py-0.5 text-xs font-semibold',
        {
          'border-border bg-muted text-muted-foreground': tone === 'neutral',
          'border-primary/30 bg-primary/10 text-primary': tone === 'primary',
          'border-success/30 bg-success/10 text-success': tone === 'success',
          'border-guest/30 bg-guest/10 text-guest': tone === 'warning',
          'border-destructive/30 bg-destructive/10 text-destructive':
            tone === 'danger',
        },
        className
      )}
    >
      {children}
    </span>
  );
}

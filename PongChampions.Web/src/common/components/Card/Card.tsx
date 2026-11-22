import clsx from 'clsx';
import type { ReactNode } from 'react';

interface ICardProps {
  title?: string;
  description?: ReactNode;
  children?: ReactNode;
  className?: string;
}

export function Card({ title, description, children, className }: ICardProps) {
  return (
    <section
      className={clsx(
        'flex flex-col gap-4 rounded-2xl border border-border bg-card p-5 text-card-foreground shadow-sm sm:p-6',
        className
      )}
    >
      {(title || description) && (
        <header className="flex flex-col gap-1">
          {title && <h2 className="text-lg font-semibold">{title}</h2>}
          {description && (
            <p className="text-sm text-muted-foreground">{description}</p>
          )}
        </header>
      )}
      {children}
    </section>
  );
}

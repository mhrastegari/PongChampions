import clsx from 'clsx';
import type { ReactNode } from 'react';

interface IContainerProps {
  children: ReactNode;
  className?: string;
}

// Page gutter that also clears notches in landscape (the app is LTR only).
export function Container({ children, className }: IContainerProps) {
  return (
    <div
      className={clsx(
        'mx-auto w-full max-w-5xl pl-[max(1rem,env(safe-area-inset-left))] pr-[max(1rem,env(safe-area-inset-right))]',
        'sm:pl-[max(1.5rem,env(safe-area-inset-left))] sm:pr-[max(1.5rem,env(safe-area-inset-right))]',
        className
      )}
    >
      {children}
    </div>
  );
}

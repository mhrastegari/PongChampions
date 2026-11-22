import type { ReactNode } from 'react';
import { Card } from '@components/Card';

interface IAuthPanelProps {
  title: string;
  description: string;
  children: ReactNode;
  footer: ReactNode;
}

// The shared frame of the login and registration screens.
export function AuthPanel({
  title,
  description,
  children,
  footer,
}: IAuthPanelProps) {
  return (
    <div className="mx-auto flex w-full max-w-md flex-col gap-4">
      <div className="flex flex-col gap-1 text-center">
        <h1 className="text-3xl font-black tracking-tight">{title}</h1>
        <p className="text-muted-foreground">{description}</p>
      </div>
      <Card>{children}</Card>
      <p className="text-center text-sm text-muted-foreground">{footer}</p>
    </div>
  );
}

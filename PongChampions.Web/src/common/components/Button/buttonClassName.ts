import clsx from 'clsx';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';

export type ButtonSize = 'md' | 'lg';

export interface IButtonStyle {
  variant?: ButtonVariant;
  size?: ButtonSize;
  className?: string;
}

// Shared by <Button> and router links styled as buttons.
export function buttonClassName({
  variant = 'primary',
  size = 'md',
  className,
}: IButtonStyle = {}) {
  return clsx(
    'inline-flex min-h-11 min-w-11 select-none items-center justify-center gap-2 rounded-lg font-semibold transition-colors',
    'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background',
    'disabled:pointer-events-none disabled:opacity-50',
    {
      'bg-primary text-primary-foreground hover:bg-primary/90':
        variant === 'primary',
      'border border-border bg-secondary text-secondary-foreground hover:bg-accent':
        variant === 'secondary',
      'text-foreground hover:bg-accent hover:text-accent-foreground':
        variant === 'ghost',
      'bg-destructive text-destructive-foreground hover:bg-destructive/90':
        variant === 'danger',
      'px-4 text-sm': size === 'md',
      'min-h-12 px-6 text-base': size === 'lg',
    },
    className
  );
}

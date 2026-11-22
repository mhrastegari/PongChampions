import { forwardRef, type ButtonHTMLAttributes } from 'react';
import { Spinner } from '@components/Spinner';
import { buttonClassName, type IButtonStyle } from './buttonClassName';

interface IButtonProps
  extends ButtonHTMLAttributes<HTMLButtonElement>, Omit<IButtonStyle, 'className'> {
  loading?: boolean;
}

export const Button = forwardRef<HTMLButtonElement, IButtonProps>(function Button(
  {
    variant,
    size,
    loading = false,
    className,
    disabled,
    children,
    type = 'button',
    ...props
  },
  ref
) {
  return (
    <button
      ref={ref}
      type={type}
      className={buttonClassName({ variant, size, className })}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...props}
    >
      {loading && <Spinner className="size-4" />}
      {children}
    </button>
  );
});

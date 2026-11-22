import clsx from 'clsx';

interface ILogoMarkProps {
  className?: string;
}

export function LogoMark({ className }: ILogoMarkProps) {
  return (
    <svg
      viewBox="0 0 32 32"
      className={clsx('size-8 shrink-0', className)}
      aria-hidden="true"
    >
      <rect width="32" height="32" rx="7" className="fill-court" />
      <rect x="5" y="9" width="3" height="14" rx="1.5" className="fill-host" />
      <rect x="24" y="5" width="3" height="14" rx="1.5" className="fill-guest" />
      <circle cx="17" cy="19" r="3" className="fill-court-foreground" />
    </svg>
  );
}

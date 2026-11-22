// Decorative miniature court; the real one is drawn on canvas in a room.
export function HeroCourt() {
  return (
    <div
      aria-hidden="true"
      className="relative aspect-court w-full overflow-hidden rounded-3xl border border-border bg-court shadow-xl"
    >
      <svg viewBox="0 0 160 100" className="absolute inset-0 size-full">
        <line
          x1="80"
          y1="3"
          x2="80"
          y2="100"
          className="stroke-court-foreground/20"
          strokeWidth="0.8"
          strokeDasharray="3.5 3.5"
        />
        <rect x="9" y="28" width="2.2" height="24" rx="1.1" className="fill-host" />
        <rect
          x="148.8"
          y="46"
          width="2.2"
          height="24"
          rx="1.1"
          className="fill-guest"
        />
        <path
          d="M14 40 L63 12 L112 50"
          className="stroke-court-foreground/15"
          strokeWidth="0.6"
          strokeDasharray="1.5 2"
          fill="none"
        />
        <circle cx="112" cy="50" r="1.6" className="fill-court-foreground" />
      </svg>
      <div className="absolute inset-x-0 top-3 flex justify-center gap-10 font-mono text-2xl font-black tabular-nums sm:top-5 sm:text-3xl">
        <span className="text-host">3</span>
        <span className="text-guest">2</span>
      </div>
    </div>
  );
}

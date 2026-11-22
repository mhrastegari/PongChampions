import { Link } from '@tanstack/react-router';
import { buttonClassName } from '@components/Button';
import { useDocumentTitle } from '@hooks/useDocumentTitle';
import { roomService } from '@services/rooms';
import { vocabService } from '@services/vocab';
import { HeroCourt } from './HeroCourt';
import { HostMatchCard } from './HostMatchCard';
import { JoinMatchCard } from './JoinMatchCard';

const { format, get: vocab } = vocabService;

const steps = [
  { title: 'step_host_title', text: 'step_host_text' },
  { title: 'step_share_title', text: 'step_share_text' },
  { title: 'step_play_title', text: 'step_play_text' },
] satisfies { title: string; text: string }[];

export function HomePage() {
  useDocumentTitle();

  const activeRoom = roomService.getActiveRoom();

  return (
    <div className="flex flex-col gap-10 sm:gap-14">
      <section className="grid items-center gap-8 md:grid-cols-[1.1fr_1fr]">
        <div className="flex flex-col gap-4">
          <p className="text-sm font-semibold uppercase tracking-widest text-primary">
            {vocab('home_eyebrow')}
          </p>
          <h1 className="text-display font-black tracking-tight">
            {vocab('home_title')}
          </h1>
          <p className="max-w-prose text-base text-muted-foreground sm:text-lg">
            {vocab('home_subtitle')}
          </p>
        </div>
        <HeroCourt />
      </section>

      {activeRoom && (
        <div
          role="status"
          className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-primary/40 bg-primary/10 px-4 py-3 sm:px-5"
        >
          <p className="text-sm">{format('active_room_notice', activeRoom)}</p>
          <Link
            to="/rooms/$code"
            params={{ code: activeRoom }}
            className={buttonClassName()}
          >
            {vocab('active_room_return')}
          </Link>
        </div>
      )}

      <div className="grid gap-4 sm:gap-6 md:grid-cols-2">
        <HostMatchCard />
        <JoinMatchCard />
      </div>

      <section aria-labelledby="how-it-works" className="flex flex-col gap-4">
        <h2 id="how-it-works" className="text-xl font-bold">
          {vocab('how_it_works')}
        </h2>
        <ol className="grid gap-4 sm:grid-cols-3">
          {steps.map((step, index) => (
            <li
              key={step.title}
              className="flex gap-3 rounded-2xl border border-border p-4"
            >
              <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-muted font-mono text-sm font-bold">
                {index + 1}
              </span>
              <div className="flex flex-col gap-1">
                <h3 className="font-semibold">{vocab(step.title)}</h3>
                <p className="text-sm text-muted-foreground">{vocab(step.text)}</p>
              </div>
            </li>
          ))}
        </ol>
      </section>
    </div>
  );
}

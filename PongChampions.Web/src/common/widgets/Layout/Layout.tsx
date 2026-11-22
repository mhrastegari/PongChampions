import { Link, Outlet } from '@tanstack/react-router';
import { Button, buttonClassName } from '@components/Button';
import { Container } from '@components/Container';
import { LogoMark } from '@components/LogoMark';
import { useAuth } from '@hooks/useAuth';
import { authService } from '@services/auth';
import { vocabService } from '@services/vocab';
import { ThemeToggle } from '@widgets/ThemeToggle';

const { format, get: vocab } = vocabService;

export function Layout() {
  const user = useAuth();

  return (
    <div className="flex min-h-dvh flex-col">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:fixed focus:start-4 focus:top-[calc(1rem+env(safe-area-inset-top))] focus:z-50 focus:rounded-lg focus:bg-primary focus:px-4 focus:py-2 focus:text-primary-foreground"
      >
        {vocab('skip_to_content')}
      </a>

      <header className="sticky top-0 z-30 border-b border-border/70 bg-background/90 pt-safe-t backdrop-blur">
        <Container className="flex h-16 items-center justify-between gap-2">
          <Link
            to="/"
            className="flex min-h-11 items-center gap-2 rounded-lg font-bold tracking-tight focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            <LogoMark />
            <span className="hidden sm:inline">{vocab('app_name')}</span>
            <span className="sr-only sm:hidden">{vocab('app_name')}</span>
          </Link>

          <nav
            aria-label={vocab('nav_label')}
            className="flex items-center gap-1 sm:gap-2"
          >
            {user ? (
              <>
                <span
                  className="hidden max-w-48 truncate text-sm text-muted-foreground sm:inline"
                  title={user.username}
                >
                  {format('signed_in_as', user.username)}
                </span>
                <Button variant="ghost" onClick={authService.logout}>
                  {vocab('logout')}
                </Button>
              </>
            ) : (
              <>
                <Link to="/login" className={buttonClassName({ variant: 'ghost' })}>
                  {vocab('login')}
                </Link>
                <Link to="/register" className={buttonClassName()}>
                  {vocab('register')}
                </Link>
              </>
            )}
            <ThemeToggle />
          </nav>
        </Container>
      </header>

      <main id="main" className="flex-1 py-6 sm:py-10">
        <Container>
          <Outlet />
        </Container>
      </main>

      <footer className="border-t border-border/70 pb-[calc(1.5rem+env(safe-area-inset-bottom))] pt-6 text-sm text-muted-foreground">
        <Container>{vocab('footer_tagline')}</Container>
      </footer>
    </div>
  );
}

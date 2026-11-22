import { createRootRoute, createRoute, createRouter } from '@tanstack/react-router';
import { safeRedirect } from '@core/utils';
import { HomePage } from '@pages/HomePage';
import { LoginPage } from '@pages/LoginPage';
import { NotFoundPage } from '@pages/NotFoundPage';
import { RegisterPage } from '@pages/RegisterPage';
import { RoomPage } from '@pages/RoomPage';
import { Layout } from '@widgets/Layout';

interface IAuthSearch {
  redirect?: string;
}

function validateAuthSearch(search: Record<string, unknown>): IAuthSearch {
  const redirect = safeRedirect(search.redirect);

  return redirect ? { redirect } : {};
}

const rootRoute = createRootRoute({
  component: Layout,
  notFoundComponent: () => <NotFoundPage />,
});

const homeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: HomePage,
});

const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  validateSearch: validateAuthSearch,
  component: LoginPage,
});

const registerRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/register',
  validateSearch: validateAuthSearch,
  component: RegisterPage,
});

const roomRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/rooms/$code',
  component: RoomPage,
});

const routeTree = rootRoute.addChildren([
  homeRoute,
  loginRoute,
  registerRoute,
  roomRoute,
]);

export const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
  scrollRestoration: true,
});

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}

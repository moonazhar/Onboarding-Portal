import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { describe, expect, it } from 'vitest';

import { AppProviders } from './AppProviders';
import { routes } from './routes';

function renderAt(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  render(
    <AppProviders>
      <RouterProvider router={router} />
    </AppProviders>,
  );
}

describe('routes', () => {
  it('shows the app header and the home page at /', async () => {
    renderAt('/');

    expect((await screen.findByRole('heading', { level: 1 })).textContent).toBe(
      'Onboarding Portal',
    );
    expect(screen.getByRole('heading', { level: 2 }).textContent).toBe('Welcome');
  });

  it('shows the not-found page for an unknown path', async () => {
    renderAt('/no-such-page');

    expect((await screen.findByRole('heading', { level: 2 })).textContent).toBe('Page not found');
    expect(screen.getByRole('link', { name: 'Go to the home page' }).getAttribute('href')).toBe(
      '/',
    );
  });
});

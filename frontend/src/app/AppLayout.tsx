import { AppShell, Group, Title } from '@mantine/core';
import { Outlet } from 'react-router';

export function AppLayout() {
  return (
    <AppShell header={{ height: 56 }} padding="md">
      <AppShell.Header>
        <Group h="100%" px="md">
          <Title order={1} size="h3">
            Onboarding Portal
          </Title>
        </Group>
      </AppShell.Header>
      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}

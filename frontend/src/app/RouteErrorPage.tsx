import { Anchor, Container, Stack, Text, Title } from '@mantine/core';
import { Link } from 'react-router';

// Shown when a page throws while rendering. Error details stay in the browser console, never on screen.
export function RouteErrorPage() {
  return (
    <Container py="xl">
      <Stack>
        <Title order={2}>Something went wrong</Title>
        <Text>Reload the page. If it keeps happening, contact support.</Text>
        <Anchor component={Link} to="/">
          Go to the home page
        </Anchor>
      </Stack>
    </Container>
  );
}

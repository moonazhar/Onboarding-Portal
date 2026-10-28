import { Anchor, Stack, Text, Title } from '@mantine/core';
import { Link } from 'react-router';

export function NotFoundPage() {
  return (
    <Stack>
      <Title order={2}>Page not found</Title>
      <Text>The page you asked for doesn't exist or has moved.</Text>
      <Anchor component={Link} to="/">
        Go to the home page
      </Anchor>
    </Stack>
  );
}

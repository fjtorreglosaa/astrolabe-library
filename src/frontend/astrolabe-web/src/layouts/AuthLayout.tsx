import { Box, Container, Stack, Typography } from '@mui/material';
import { Outlet } from 'react-router-dom';
import { elevation, fonts, radii, rem, typeScale } from '../theme/tokens';

/**
 * Layout for sign-in, sign-up and verification. The prototype deliberately drops the navbar and
 * sidebar on these screens, keeping the page focused on a single action.
 */
export const AuthLayout = () => (
  <Box
    sx={{
      minHeight: '100vh',
      display: 'flex',
      flexDirection: 'column',
      bgcolor: 'background.default',
    }}
  >
    <Container maxWidth="sm" sx={{ flex: 1, display: 'flex', alignItems: 'center', py: 6 }}>
      <Stack spacing={4} sx={{ width: '100%' }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', justifyContent: 'center' }}>
          <Box
            aria-hidden
            sx={{
              width: 40,
              height: 40,
              borderRadius: `${radii.input}px`,
              bgcolor: 'primary.main',
              color: 'primary.contrastText',
              display: 'grid',
              placeItems: 'center',
              fontFamily: fonts.display,
              fontSize: rem(typeScale.heading),
              fontWeight: 600,
              boxShadow: elevation.primary,
            }}
          >
            A
          </Box>
          <Typography variant="h3">
            Astrolabe Books
          </Typography>
        </Stack>

        <Outlet />
      </Stack>
    </Container>

    <Box component="footer" sx={{ py: 3, textAlign: 'center' }}>
      {/* Sentence case, matching the prototype and the app footer. `overline` is the theme's
          uppercase micro-label and turned this line into a shouted heading. */}
      <Typography variant="caption" color="text.secondary">
        © 2026 Astrolabe Books · New York · Chicago · Austin
      </Typography>
    </Box>
  </Box>
);

import { Box, Link, Stack, Typography } from '@mui/material';

/** Footer shown on every authenticated screen, matching the prototype. */
export const AppFooter = () => (
  <Box
    component="footer"
    sx={{ px: 3, py: 2, borderTop: 1, borderColor: 'divider', bgcolor: 'background.paper' }}
  >
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      spacing={{ xs: 1, sm: 3 }}
      sx={{ alignItems: { xs: 'flex-start', sm: 'center' }, justifyContent: 'space-between' }}
    >
      <Typography variant="overline" color="text.secondary">
        © 2026 Astrolabe Books
      </Typography>
      <Stack direction="row" spacing={2}>
        {['Terms', 'Privacy', 'Help', 'API'].map((label) => (
          <Link key={label} href="#" variant="overline" color="text.secondary" underline="hover">
            {label}
          </Link>
        ))}
      </Stack>
    </Stack>
  </Box>
);

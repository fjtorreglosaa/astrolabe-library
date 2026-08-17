import { Box, Link, Stack, Typography } from '@mui/material';

/** The four links, in the prototype's order. Placeholders until each has somewhere to go. */
const FOOTER_LINKS = ['Terms', 'Privacy', 'Help', 'API'] as const;

/**
 * The footer on every authenticated screen.
 *
 * <p>
 * Sentence case, in `caption`, not `overline`. The theme defines `overline` as uppercase with wide
 * tracking — the micro-label signature used for section kickers — and applying it here rendered the
 * prototype's "© 2026 Astrolabe Books · Terms · Privacy" as "© 2026 ASTROLABE BOOKS · TERMS ·
 * PRIVACY". A footer set in the same treatment as a section heading competes with the page above it,
 * which is the opposite of what a footer is for.
 * </p>
 */
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
      <Typography variant="caption" color="text.secondary">
        © 2026 Astrolabe Books
      </Typography>

      <Stack direction="row" spacing={2.5}>
        {FOOTER_LINKS.map((label) => (
          <Link
            key={label}
            href="#"
            variant="caption"
            color="text.secondary"
            underline="hover"
            // These lead nowhere yet. Marking them so assistive technology does not announce four
            // navigation targets that are not: the prototype shows them, and hiding them would be a
            // bigger departure than saying plainly that they are not wired up.
            aria-disabled="true"
          >
            {label}
          </Link>
        ))}
      </Stack>
    </Stack>
  </Box>
);

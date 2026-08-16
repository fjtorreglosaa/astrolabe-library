import { Chip, Paper, Stack, Typography } from '@mui/material';

/**
 * Stands in for a screen that has not been built yet.
 *
 * It exists so the shell is navigable and reviewable against the prototype during Stage 0, and is
 * deleted as each screen lands. It deliberately looks unfinished so it can never be mistaken for a
 * delivered screen.
 */
export const PlaceholderScreen = ({ title, stage }: { title: string; stage: string }) => (
  <Paper variant="outlined" sx={{ p: 4 }}>
    <Stack spacing={1.5} sx={{ alignItems: 'flex-start' }}>
      <Chip label={`Not built yet · ${stage}`} size="small" color="warning" variant="outlined" />
      <Typography variant="h4">{title}</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 560 }}>
        This screen is specified in the prototype at <code>docs/design/</code> and will be built in
        its stage. The shell around it — navbar, sidebar, footer and theme — is real.
      </Typography>
    </Stack>
  </Paper>
);

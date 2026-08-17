import { Box, Paper, Skeleton, Stack } from '@mui/material';

/**
 * The shape of the dashboard's metric tiles.
 *
 * <p>
 * Its own component rather than a `TableSkeleton` with different numbers, because a stat tile is not
 * a short row: it is a label, a large figure and a note, and a skeleton that suggested rows would
 * mislead about what is coming. The prototype loads `stats` separately from `loans` for the same
 * reason — they arrive at different times and look nothing alike.
 * </p>
 */
export const StatCardsSkeleton = ({ count = 4 }: { count?: number }) => (
  <Box
    role="status"
    aria-label="Loading your figures"
    aria-busy="true"
    sx={{
      display: 'grid',
      gap: 2,
      gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: `repeat(${count}, 1fr)` },
    }}
  >
    {Array.from({ length: count }, (_, index) => (
      <Paper key={index} variant="outlined" sx={{ p: 2 }}>
        <Stack spacing={1}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <Skeleton variant="circular" width={22} height={22} />
            <Skeleton variant="text" sx={{ width: '60%', fontSize: 12 }} />
          </Stack>
          {/* The figure is the point of the tile, so it is the one bar drawn at its real size. */}
          <Skeleton variant="text" sx={{ width: '40%', fontSize: 32 }} />
          <Skeleton variant="text" sx={{ width: '75%', fontSize: 12 }} />
        </Stack>
      </Paper>
    ))}
  </Box>
);

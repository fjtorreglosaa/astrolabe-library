import { Box, Skeleton, Stack } from '@mui/material';

/**
 * The shape of a grid of book cards that has not arrived yet.
 *
 * <p>
 * The cover block is drawn at <b>3:4</b>, the ratio the real cards crop to. That is the whole
 * value of this over a spinner: the grid occupies its final height immediately, so the covers
 * arriving do not shove the page down under whoever was already reading it.
 * </p>
 */
export interface CardGridSkeletonProps {
  count?: number;
  label?: string;
}

export const CardGridSkeleton = ({
  count = 8,
  label = 'Loading books',
}: CardGridSkeletonProps) => (
  <Box
    role="status"
    aria-label={label}
    aria-busy="true"
    sx={{
      display: 'grid',
      gap: 2,
      gridTemplateColumns: {
        xs: 'repeat(2, 1fr)',
        sm: 'repeat(3, 1fr)',
        md: 'repeat(4, 1fr)',
      },
    }}
  >
    {Array.from({ length: count }, (_, index) => (
      <Stack key={index} spacing={1}>
        <Skeleton variant="rounded" sx={{ width: '100%', aspectRatio: '3 / 4', height: 'auto' }} />
        <Skeleton variant="text" sx={{ width: '85%', fontSize: 14 }} />
        <Skeleton variant="text" sx={{ width: '55%', fontSize: 12 }} />
      </Stack>
    ))}
  </Box>
);

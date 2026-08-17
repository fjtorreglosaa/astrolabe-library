import { Skeleton, Stack } from '@mui/material';

/**
 * The shape of a stacked list — tickets, notifications, a profile's label-and-value rows.
 *
 * <p>
 * No outline of its own, so it can sit inside a panel that already has one. A skeleton that drew a
 * second border inside the card it is filling would be a shape the real content never takes.
 * </p>
 */
export interface ListRowsSkeletonProps {
  rows?: number;
  /** Draws a small round block on the left, for lists whose rows carry an icon or avatar. */
  leading?: boolean;
  label?: string;
}

export const ListRowsSkeleton = ({
  rows = 4,
  leading = true,
  label = 'Loading',
}: ListRowsSkeletonProps) => (
  <Stack spacing={2} role="status" aria-label={label} aria-busy="true">
    {Array.from({ length: rows }, (_, index) => (
      <Stack key={index} direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
        {leading ? <Skeleton variant="circular" width={32} height={32} /> : null}
        <Stack spacing={0.5} sx={{ flex: 1, minWidth: 0 }}>
          {/* Descending widths, so a column of rows reads as text rather than as a bar chart. */}
          <Skeleton variant="text" sx={{ width: `${70 - (index % 3) * 12}%`, fontSize: 15 }} />
          <Skeleton variant="text" sx={{ width: `${45 - (index % 3) * 8}%`, fontSize: 12 }} />
        </Stack>
      </Stack>
    ))}
  </Stack>
);

import { Paper, Skeleton, Stack } from '@mui/material';

/**
 * The shape of a table that has not arrived yet.
 *
 * <p>
 * A skeleton rather than a spinner because the two answer different questions. A spinner says
 * "something is happening"; a skeleton says "a table with about five rows is about to appear here",
 * and the page stops jumping when it does. The prototype makes the same distinction — it holds
 * `skel:[1,2,3,4,5]` for lists and keeps its spinners inside buttons.
 * </p>
 * <p>
 * Row counts come from the prototype: five for a full table, four for a compact one, three for a
 * panel. They are a guess at the real content, and a good guess is what stops the layout shifting.
 * </p>
 */
export interface TableSkeletonProps {
  /** How many rows to suggest. The prototype uses 5, 4 or 3 depending on the surface. */
  rows?: number;
  /** Whether to draw a header strip above them. */
  header?: boolean;
  /** Aria label for the live region. Says what is loading, not that something is. */
  label?: string;
}

export const TableSkeleton = ({
  rows = 5,
  header = true,
  label = 'Loading results',
}: TableSkeletonProps) => (
  <Paper variant="outlined" role="status" aria-label={label} aria-busy="true">
    {header ? (
      <Stack
        direction="row"
        spacing={2}
        sx={{ px: 2, py: 1.5, borderBottom: 1, borderColor: 'divider' }}
      >
        {[38, 22, 18, 14].map((width, index) => (
          <Skeleton key={index} variant="text" sx={{ width: `${width}%`, fontSize: 14 }} />
        ))}
      </Stack>
    ) : null}

    <Stack divider={<Stack sx={{ borderBottom: 1, borderColor: 'divider' }} />}>
      {Array.from({ length: rows }, (_, index) => (
        <Stack
          key={index}
          direction="row"
          spacing={2}
          sx={{ px: 2, py: 1.75, alignItems: 'center' }}
        >
          {/* Uneven widths on purpose. Four identical bars read as a loading graphic; four unequal
              ones read as a row of real columns waiting to be filled. */}
          <Skeleton variant="text" sx={{ width: '38%', fontSize: 16 }} />
          <Skeleton variant="text" sx={{ width: '22%', fontSize: 16 }} />
          <Skeleton variant="text" sx={{ width: '18%', fontSize: 16 }} />
          <Skeleton variant="rounded" width={72} height={24} />
        </Stack>
      ))}
    </Stack>
  </Paper>
);

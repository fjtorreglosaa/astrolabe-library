import { IconButton, MenuItem, Select, Stack, Typography } from '@mui/material';
import { MaterialSymbol } from './MaterialSymbol';

/** The sizes the prototype offers. */
export const ROWS_PER_PAGE = [5, 10, 25] as const;

/**
 * The bar under a table: how many rows, which page, and the two arrows.
 *
 * <p>
 * It belongs <b>inside</b> the table's own panel, above its bottom edge, which is where the
 * prototype puts it — a pager floating below a card reads as a separate control that happens to sit
 * nearby, rather than as part of the thing it pages.
 * </p>
 * <p>
 * "Rows per page" is the part that was missing entirely. A member with two hundred results and a
 * fixed page of twelve has seventeen pages and no way to change that; the prototype lets them ask
 * for twenty-five and be done.
 * </p>
 */
export interface TablePagerBarProps {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

export const TablePagerBar = ({
  page,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
  onPageSizeChange,
}: TablePagerBarProps) => {
  const first = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const last = Math.min(page * pageSize, totalCount);

  return (
    <Stack
      direction="row"
      spacing={1.75}
      sx={{
        alignItems: 'center',
        flexWrap: 'wrap',
        rowGap: 1,
        px: 2.5,
        py: 1.5,
        borderTop: 1,
        borderColor: 'divider',
      }}
    >
      <Typography variant="caption" color="text.secondary">
        Rows per page
      </Typography>

      <Select
        size="small"
        value={pageSize}
        onChange={(event) => {
          // Back to the first page. Staying on page nine while the pages get bigger lands somebody
          // past the end of their own results, which reads as "everything disappeared".
          onPageSizeChange(Number(event.target.value));
          onPageChange(1);
        }}
        inputProps={{ 'aria-label': 'Rows per page' }}
        sx={{ height: 30, borderRadius: '15px', fontSize: 12 }}
      >
        {ROWS_PER_PAGE.map((size) => (
          <MenuItem key={size} value={size}>
            {size}
          </MenuItem>
        ))}
      </Select>

      <Typography variant="caption" color="text.secondary">
        Page {page} of {Math.max(totalPages, 1)}
      </Typography>

      <Typography variant="caption" color="text.secondary" sx={{ ml: 'auto' }}>
        {totalCount === 0 ? 'No results' : `${first}–${last} of ${totalCount}`}
      </Typography>

      <IconButton
        aria-label="Previous page"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
        sx={{ width: 32, height: 32, border: 1, borderColor: 'divider' }}
      >
        <MaterialSymbol name="chevron_left" size={18} />
      </IconButton>

      <IconButton
        aria-label="Next page"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
        sx={{ width: 32, height: 32, border: 1, borderColor: 'divider' }}
      >
        <MaterialSymbol name="chevron_right" size={18} />
      </IconButton>
    </Stack>
  );
};

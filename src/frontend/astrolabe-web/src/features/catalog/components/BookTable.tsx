import {
  Button,
  Chip,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TableSortLabel,
  Typography,
} from '@mui/material';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import type { Membership } from '../../membership/api/membershipApi';
import { money } from '../../membership/planCopy';
import type { BookSortKey, BookSummary, SortDirection } from '../api/catalogApi';
import { GENRE_LABEL, availabilityLabel, bookBadgeLabel } from '../catalogCopy';

/**
 * The catalogue as a table, the prototype's second view.
 *
 * It carries the same information as the cards and the same verdict, so switching view can never
 * change what a member is told they may borrow.
 */
export interface BookTableProps {
  books: BookSummary[];
  membership: Membership | undefined;
  sortBy: BookSortKey;
  direction: SortDirection;
  onSort: (key: BookSortKey) => void;
  onOpen: (book: BookSummary) => void;
}

/** The sortable columns and their alignment, in the prototype's own order. */
const COLUMNS: { key: BookSortKey; label: string; align?: 'right' }[] = [
  { key: 'Title', label: 'Title' },
  { key: 'Author', label: 'Author' },
  { key: 'Genre', label: 'Genre' },
  { key: 'Tier', label: 'Plan' },
  { key: 'Availability', label: 'Availability' },
  { key: 'Rating', label: 'Rating', align: 'right' },
  { key: 'Price', label: 'Price', align: 'right' },
];

export const BookTable = ({
  books,
  membership,
  sortBy,
  direction,
  onSort,
  onOpen,
}: BookTableProps) => (
  <Table size="small">
    <TableHead>
      <TableRow>
        {COLUMNS.map((column) => (
          <TableCell
            key={column.key}
            align={column.align}
            sortDirection={sortBy === column.key ? (direction === 'Ascending' ? 'asc' : 'desc') : false}
          >
            {/* Clicking a header sorts by it; clicking the active one reverses it, as the
                prototype does. */}
            <TableSortLabel
              active={sortBy === column.key}
              direction={direction === 'Ascending' ? 'asc' : 'desc'}
              onClick={() => onSort(column.key)}
            >
              {column.label}
            </TableSortLabel>
          </TableCell>
        ))}
        <TableCell align="right" />
      </TableRow>
    </TableHead>

    <TableBody>
      {books.map((book) => (
        <TableRow key={book.id} hover sx={{ cursor: 'pointer' }} onClick={() => onOpen(book)}>
          <TableCell>
            <Typography variant="body2">{book.title}</Typography>
          </TableCell>
          <TableCell>
            <Typography variant="body2" color="text.secondary">
              {book.author}
            </Typography>
          </TableCell>
          <TableCell>
            <Typography variant="body2" color="text.secondary">
              {GENRE_LABEL[book.genre]}
            </Typography>
          </TableCell>
          <TableCell>
            <Chip size="small" variant="outlined" label={book.tier} />
          </TableCell>
          <TableCell>
            <Stack spacing={0.5}>
              <Typography variant="body2">{availabilityLabel(book.availableCount)}</Typography>
              {book.badge ? (
                <Chip size="small" color="warning" label={bookBadgeLabel(book.badge, membership)} />
              ) : null}
            </Stack>
          </TableCell>
          <TableCell align="right">
            {book.averageRating === null ? (
              <Typography variant="body2" color="text.secondary">
                —
              </Typography>
            ) : (
              <Stack
                direction="row"
                spacing={0.25}
                sx={{ alignItems: 'center', justifyContent: 'flex-end' }}
              >
                <MaterialSymbol name="star" size={14} fill={1} sx={{ color: 'warning.main' }} />
                <Typography variant="body2">{book.averageRating.toFixed(1)}</Typography>
              </Stack>
            )}
          </TableCell>
          <TableCell align="right">
            <Typography variant="body2">{money(book.retailPriceCents)}</Typography>
          </TableCell>
          <TableCell align="right">
            <Button
              size="small"
              variant={book.canReserve ? 'contained' : 'outlined'}
              disabled={!book.canReserve}
              onClick={(event) => {
                // The row already opens the book; without this the click would fire twice.
                event.stopPropagation();
                onOpen(book);
              }}
            >
              {book.canReserve ? 'Reserve' : 'Unavailable'}
            </Button>
          </TableCell>
        </TableRow>
      ))}
    </TableBody>
  </Table>
);

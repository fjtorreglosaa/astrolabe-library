import {
  Button,
  Chip,
  Box,
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
import { BookCover } from './BookCover';
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
  /** Starts a reservation directly, without opening the panel first. */
  onReserve: (book: BookSummary) => void;
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
  onReserve,
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
            <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
              <Box sx={{ width: 28, flexShrink: 0 }}>
                <BookCover
                  bookId={book.id}
                  title={book.title}
                  coverUrl={book.coverUrl}
                  height={40}
                />
              </Box>
              <Typography variant="body2" sx={{ fontWeight: 600, minWidth: 0 }}>
                {book.title}
              </Typography>
            </Stack>
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
            <Chip
              size="small"
              label={book.tier}
              sx={{ bgcolor: 'rgba(14,90,110,.10)', color: 'primary.main' }}
            />
          </TableCell>
          <TableCell>
            <Stack spacing={0.5}>
              <Typography variant="body2">{availabilityLabel(book.availableCount)}</Typography>
              {book.badge ? (
                // The same locked strip the card uses, so a refusal reads identically in both
                // views rather than as two different states.
                <Stack
                  direction="row"
                  spacing={0.5}
                  sx={{
                    alignSelf: 'flex-start',
                    alignItems: 'center',
                    px: 1,
                    py: 0.25,
                    borderRadius: '10px',
                    bgcolor: 'rgba(179,38,30,.10)',
                    color: '#B3261E',
                  }}
                >
                  <MaterialSymbol name="lock" size={13} />
                  <Typography variant="caption" sx={{ fontWeight: 600 }}>
                    {bookBadgeLabel(book.badge, membership)}
                  </Typography>
                </Stack>
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
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
              {money(book.retailPriceCents)}
            </Typography>
          </TableCell>
          <TableCell align="right">
            <Stack direction="row" spacing={0.75} sx={{ justifyContent: 'flex-end' }}>
              <Button
                size="small"
                variant="outlined"
                color="inherit"
                onClick={(event) => {
                  // The row already opens the book; without this the click would fire twice.
                  event.stopPropagation();
                  onOpen(book);
                }}
                sx={{ height: 32, whiteSpace: 'nowrap' }}
              >
                Details
              </Button>
              <Button
                size="small"
                variant={book.canReserve ? 'contained' : 'outlined'}
                disabled={!book.canReserve}
                onClick={(event) => {
                  event.stopPropagation();
                  // It used to open the panel instead — a control labelled "Reserve" that did
                  // something else, which is the one thing a button must never do.
                  onReserve(book);
                }}
                sx={{ height: 32, whiteSpace: 'nowrap' }}
              >
                {book.canReserve ? 'Reserve' : 'Unavailable'}
              </Button>
            </Stack>
          </TableCell>
        </TableRow>
      ))}
    </TableBody>
  </Table>
);

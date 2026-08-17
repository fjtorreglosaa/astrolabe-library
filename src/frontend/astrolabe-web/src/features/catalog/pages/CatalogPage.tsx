import {
  Box,
  Chip,
  Pagination,
  Paper,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState } from '../../../shared/components/StateViews';
import { CardGridSkeleton } from '../../../shared/components/CardGridSkeleton';
import { TablePagerBar } from '../../../shared/components/TablePagerBar';
import { getMyMembership } from '../../membership/api/membershipApi';
import {
  searchBooks,
  type BookSortKey,
  type BookSummary,
  type Genre,
  type SortDirection,
} from '../api/catalogApi';
import { GENRE_FILTERS, GENRE_LABEL } from '../catalogCopy';
import { BookCard } from '../components/BookCard';
import { BookDetailDialog } from '../components/BookDetailDialog';
import { BuyBookDialog } from '../../store/components/BuyBookDialog';
import { ReserveDialog } from '../../reservations/components/ReserveDialog';
import { BookTable } from '../components/BookTable';

/** The grid's page. The table carries its own control and starts at ten, as the prototype does. */
const PAGE_SIZE = 12;

/**
 * The catalogue, with the prototype's two views, genre chips and search.
 *
 * Every row already carries its access verdict from the API, so this screen never decides whether a
 * book is reservable — it only renders the answer. That is deliberate: the same rule governs the
 * loan itself, and a second implementation here would eventually contradict it in front of a member.
 */
export const CatalogPage = () => {
  const [view, setView] = useState<'cards' | 'table'>('cards');
  const [term, setTerm] = useState('');
  const [debouncedTerm, setDebouncedTerm] = useState('');
  const [genre, setGenre] = useState<Genre | 'All'>('All');
  const [sortBy, setSortBy] = useState<BookSortKey>('Title');
  const [direction, setDirection] = useState<SortDirection>('Ascending');
  const [page, setPage] = useState(1);
  const [openBookId, setOpenBookId] = useState<string | null>(null);
  // Separate from the panel: a card can start a reservation without opening the book first.
  const [reservingBookId, setReservingBookId] = useState<string | null>(null);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);
  const [buyingBook, setBuyingBook] = useState<BookSummary | null>(null);

  // Debounced so typing a title does not fire a query per keystroke.
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedTerm(term), 300);
    return () => clearTimeout(timer);
  }, [term]);

  // Any change to the filters invalidates the current page number: staying on page 3 of a result
  // set that now has one page shows an empty screen that looks like a failure.
  useEffect(() => setPage(1), [debouncedTerm, genre, sortBy, direction]);

  // Clicking the active column reverses it; clicking another switches to it ascending, which is
  // what a reader expects from a table and what the prototype does.
  const applySort = (key: BookSortKey) => {
    if (key === sortBy) {
      setDirection((current) => (current === 'Ascending' ? 'Descending' : 'Ascending'));
      return;
    }

    setSortBy(key);
    setDirection('Ascending');
  };

  // The membership supplies the city and home library the badges name. It is not required to
  // render, so a failure here degrades the wording rather than the screen.
  const membership = useQuery({ queryKey: ['membership'], queryFn: getMyMembership });

  const books = useQuery({
    // `pageSize` is part of the key. Without it, changing the rows-per-page control would leave
    // the cached page of the old size on screen and the control would appear to do nothing.
    queryKey: ['catalog', 'books', debouncedTerm, genre, sortBy, direction, page, pageSize],
    queryFn: () =>
      searchBooks({
        term: debouncedTerm,
        genre: genre === 'All' ? undefined : genre,
        sortBy,
        direction,
        page,
        pageSize,
      }),
  });

  return (
    <Stack spacing={3}>
      <Stack spacing={0.5}>
        <Typography variant="h4">Catalog</Typography>
        <Typography variant="body2" color="text.secondary">
          Everything the network holds. What you can borrow depends on your plan and where you live.
        </Typography>
      </Stack>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack spacing={2}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: 'center' }}>
            <TextField
              size="small"
              fullWidth
              placeholder="Search by title, author, ISBN or publisher"
              value={term}
              onChange={(event) => setTerm(event.target.value)}
              slotProps={{
                input: {
                  startAdornment: (
                    <Box sx={{ display: 'flex', mr: 1, color: 'text.secondary' }}>
                      <MaterialSymbol name="search" size={20} />
                    </Box>
                  ),
                },
              }}
            />

            <ToggleButtonGroup
              size="small"
              exclusive
              value={view}
              onChange={(_event, next) => next && setView(next)}
            >
              <ToggleButton value="cards" aria-label="Card view">
                <MaterialSymbol name="grid_view" size={18} />
              </ToggleButton>
              <ToggleButton value="table" aria-label="Table view">
                <MaterialSymbol name="table_rows" size={18} />
              </ToggleButton>
            </ToggleButtonGroup>
          </Stack>

          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
            {GENRE_FILTERS.map((option) => (
              <Chip
                key={option}
                label={option === 'All' ? 'All' : GENRE_LABEL[option]}
                color={genre === option ? 'primary' : 'default'}
                variant={genre === option ? 'filled' : 'outlined'}
                onClick={() => setGenre(option)}
              />
            ))}
          </Stack>
        </Stack>
      </Paper>

      {books.isLoading ? (
        <CardGridSkeleton count={8} label="Loading the catalogue" />
      ) : books.isError || !books.data ? (
        <ErrorState
          description="We could not load the catalogue."
          onRetry={() => void books.refetch()}
        />
      ) : books.data.items.length === 0 ? (
        <EmptyState
          title="No books match this search"
          description="Try a different title, author or genre."
        />
      ) : (
        <>
          <Typography variant="caption" color="text.secondary">
            {books.data.totalCount} book{books.data.totalCount === 1 ? '' : 's'}
          </Typography>

          {view === 'cards' ? (
            <Box
              sx={{
                display: 'grid',
                gap: 2.5,
                // The prototype's own track sizing. Fixed column counts leave a wide screen with
                // four stretched cards and a narrow one with cards too thin to read a title on.
                gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))',
              }}
            >
              {books.data.items.map((book) => (
                <BookCard
                  key={book.id}
                  book={book}
                  membership={membership.data}
                  onOpen={(selected) => setOpenBookId(selected.id)}
                  // Straight to the reservation, as the prototype's card does. Opening the panel
                  // first would put a screen between a member and the thing they already chose.
                  onReserve={(selected) => setReservingBookId(selected.id)}
                />
              ))}
            </Box>
          ) : (
            <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
              <BookTable
                books={books.data.items}
                membership={membership.data}
                sortBy={sortBy}
                direction={direction}
                onSort={applySort}
                onOpen={(selected) => setOpenBookId(selected.id)}
                onReserve={(selected) => setReservingBookId(selected.id)}
              />

              <TablePagerBar
                page={books.data.page}
                pageSize={pageSize}
                totalCount={books.data.totalCount}
                totalPages={books.data.totalPages}
                onPageChange={setPage}
                onPageSizeChange={setPageSize}
              />
            </Paper>
          )}

          {/* The card view keeps the centred pager: a grid has no bottom edge to hang a bar on,
              and the prototype pages its grid the same way. */}
          {view === 'cards' && books.data.totalPages > 1 ? (
            <Stack sx={{ alignItems: 'center' }}>
              <Pagination
                count={books.data.totalPages}
                page={books.data.page}
                onChange={(_event, next) => setPage(next)}
                color="primary"
              />
            </Stack>
          ) : null}
        </>
      )}

      <ReserveDialog
        bookId={reservingBookId}
        onClose={() => setReservingBookId(null)}
        onReserved={() => setReservingBookId(null)}
      />

      <BuyBookDialog
        bookId={buyingBook?.id ?? null}
        title={buyingBook?.title ?? ''}
        coverUrl={buyingBook?.coverUrl ?? null}
        onClose={() => setBuyingBook(null)}
      />

      <BookDetailDialog
        bookId={openBookId}
        membership={membership.data}
        onClose={() => setOpenBookId(null)}
        // Both close the panel first, as the prototype does — it sets `detailId:null` in the same
        // step that opens the reservation, rather than stacking one modal on another.
        onReserve={(id) => {
          setOpenBookId(null);
          setReservingBookId(id);
        }}
        onBuy={(id) => {
          const chosen = books.data?.items.find((book) => book.id === id) ?? null;
          setOpenBookId(null);
          setBuyingBook(chosen);
        }}
      />
    </Stack>
  );
};

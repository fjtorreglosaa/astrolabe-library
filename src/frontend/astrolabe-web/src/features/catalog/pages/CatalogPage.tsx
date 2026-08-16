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
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { getMyMembership } from '../../membership/api/membershipApi';
import { searchBooks, type BookSortKey, type Genre, type SortDirection } from '../api/catalogApi';
import { GENRE_FILTERS, GENRE_LABEL } from '../catalogCopy';
import { BookCard } from '../components/BookCard';
import { BookDetailDialog } from '../components/BookDetailDialog';
import { BookTable } from '../components/BookTable';

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
    queryKey: ['catalog', 'books', debouncedTerm, genre, sortBy, direction, page],
    queryFn: () =>
      searchBooks({
        term: debouncedTerm,
        genre: genre === 'All' ? undefined : genre,
        sortBy,
        direction,
        page,
        pageSize: PAGE_SIZE,
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
        <LoadingState label="Loading the catalogue…" />
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
                gap: 2,
                gridTemplateColumns: {
                  xs: '1fr',
                  sm: 'repeat(2, 1fr)',
                  md: 'repeat(3, 1fr)',
                  lg: 'repeat(4, 1fr)',
                },
              }}
            >
              {books.data.items.map((book) => (
                <BookCard
                  key={book.id}
                  book={book}
                  membership={membership.data}
                  onOpen={(selected) => setOpenBookId(selected.id)}
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
              />
            </Paper>
          )}

          {books.data.totalPages > 1 ? (
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

      <BookDetailDialog
        bookId={openBookId}
        membership={membership.data}
        onClose={() => setOpenBookId(null)}
      />
    </Stack>
  );
};

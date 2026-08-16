import {
  Alert,
  Button,
  Chip,
  InputAdornment,
  Pagination,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TableSortLabel,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { money } from '../../membership/planCopy';
import {
  publishBook,
  restoreBook,
  returnBookFromRepair,
  searchStaffBooks,
  type BookSortKey,
  type BookStatus,
  type SortDirection,
  type StaffBook,
} from '../api/adminCatalogApi';
import {
  GENRE_LABEL,
  STATUS_COLOR,
  STATUS_FILTERS,
  STATUS_ICON,
  STATUS_LABEL,
} from '../adminCatalogCopy';
import { BookLifecycleDialog } from '../components/BookLifecycleDialog';
import { BookWizardDialog } from '../components/BookWizardDialog';

const PAGE_SIZE = 20;

const COLUMNS: { key: BookSortKey | null; label: string }[] = [
  { key: 'Title', label: 'Title' },
  { key: 'Author', label: 'Author' },
  { key: null, label: 'Genre' },
  { key: null, label: 'Tier' },
  { key: null, label: 'Status' },
  { key: 'RetailPrice', label: 'Price' },
  { key: null, label: 'Copies' },
  { key: null, label: '' },
];

/**
 * Book management.
 *
 * <p>
 * The lifecycle is the screen. A book is a draft until somebody publishes it, can go to repair and
 * come back, and can be removed and restored — and every transition out of the catalogue takes a
 * typed reason, because `BR-CAT-025` wants a trail that can still be read a year later.
 * </p>
 * <p>
 * Which books appear is the API's decision, not this screen's. Filtering by scope here would put one
 * rule in two places and the client's copy would be the one that drifts.
 * </p>
 */
export const AdminBooksPage = () => {
  const queryClient = useQueryClient();

  const [term, setTerm] = useState('');
  const [status, setStatus] = useState<BookStatus | 'All'>('All');
  const [sortBy, setSortBy] = useState<BookSortKey>('Title');
  const [direction, setDirection] = useState<SortDirection>('Ascending');
  const [page, setPage] = useState(1);
  const [wizardOpen, setWizardOpen] = useState(false);
  const [lifecycle, setLifecycle] = useState<{
    book: StaffBook;
    kind: 'repair' | 'remove';
  } | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const books = useQuery({
    queryKey: ['admin', 'catalog', term, status, sortBy, direction, page],
    queryFn: () =>
      searchStaffBooks({
        term,
        status: status === 'All' ? undefined : status,
        sortBy,
        direction,
        page,
        pageSize: PAGE_SIZE,
      }),
  });

  const transition = useMutation({
    mutationFn: async (input: { book: StaffBook; kind: 'publish' | 'return' | 'restore' }) => {
      if (input.kind === 'publish') {
        await publishBook(input.book.id);
        return `“${input.book.title}” is live in the catalogue.`;
      }

      if (input.kind === 'return') {
        await returnBookFromRepair(input.book.id);
        return `“${input.book.title}” is back on the shelves.`;
      }

      await restoreBook(input.book.id);
      return `“${input.book.title}” was restored.`;
    },
    onSuccess: async (message) => {
      setNotice(message);
      await queryClient.invalidateQueries({ queryKey: ['admin', 'catalog'] });
    },
  });

  const sortOn = (key: BookSortKey) => {
    if (sortBy === key) {
      setDirection(direction === 'Ascending' ? 'Descending' : 'Ascending');
      return;
    }

    setSortBy(key);
    setDirection('Ascending');
  };

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'flex-start' } }}
      >
        <Stack spacing={0.5}>
          <Typography variant="h4">Book management</Typography>
          <Typography variant="body2" color="text.secondary">
            Everything in your libraries, whatever state it is in.
          </Typography>
        </Stack>
        <Button
          variant="contained"
          startIcon={<MaterialSymbol name="library_add" size={20} />}
          onClick={() => setWizardOpen(true)}
        >
          Add a book
        </Button>
      </Stack>

      {notice ? (
        <Alert severity="success" onClose={() => setNotice(null)}>
          {notice}
        </Alert>
      ) : null}

      {transition.isError ? (
        <Alert severity="error" onClose={() => transition.reset()}>
          {(transition.error as { response?: { data?: { title?: string } } })?.response?.data
            ?.title ?? 'We could not complete that.'}
        </Alert>
      ) : null}

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ alignItems: 'center' }}>
        <TextField
          fullWidth
          size="small"
          placeholder="Search by title, author, ISBN or publisher"
          value={term}
          onChange={(event) => {
            setTerm(event.target.value);
            setPage(1);
          }}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <MaterialSymbol name="search" size={20} />
                </InputAdornment>
              ),
            },
          }}
        />

        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
          {STATUS_FILTERS.map((option) => (
            <Chip
              key={option}
              size="small"
              variant={status === option ? 'filled' : 'outlined'}
              color={status === option ? 'primary' : 'default'}
              label={option === 'All' ? 'All' : STATUS_LABEL[option]}
              onClick={() => {
                setStatus(option);
                setPage(1);
              }}
            />
          ))}
        </Stack>
      </Stack>

      {books.isLoading ? (
        <LoadingState label="Loading the catalogue…" />
      ) : books.isError || !books.data ? (
        <ErrorState
          description="We could not load the catalogue."
          onRetry={() => void books.refetch()}
        />
      ) : books.data.items.length === 0 ? (
        <EmptyState
          title="Nothing here"
          description={
            term || status !== 'All'
              ? 'No book matches that filter.'
              : 'Add the first book to your catalogue.'
          }
        />
      ) : (
        <>
          <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  {COLUMNS.map((column) => (
                    <TableCell key={column.label || 'actions'}>
                      {column.key ? (
                        <TableSortLabel
                          active={sortBy === column.key}
                          direction={direction === 'Ascending' ? 'asc' : 'desc'}
                          onClick={() => sortOn(column.key!)}
                        >
                          {column.label}
                        </TableSortLabel>
                      ) : (
                        column.label
                      )}
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {books.data.items.map((book) => (
                  <TableRow key={book.id} hover>
                    <TableCell>{book.title}</TableCell>
                    <TableCell>{book.author}</TableCell>
                    <TableCell>{GENRE_LABEL[book.genre]}</TableCell>
                    <TableCell>
                      <Chip size="small" variant="outlined" label={book.tier} />
                    </TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        variant="outlined"
                        color={STATUS_COLOR[book.status]}
                        icon={<MaterialSymbol name={STATUS_ICON[book.status]} size={16} />}
                        label={STATUS_LABEL[book.status]}
                      />
                    </TableCell>
                    <TableCell>{money(book.retailPriceCents)}</TableCell>
                    <TableCell>
                      {book.availableCount} / {book.totalCount}
                    </TableCell>
                    <TableCell align="right">
                      {/* Only the transitions this state actually allows. A button the aggregate
                          would refuse is a button that teaches people to distrust the screen. */}
                      <Stack direction="row" spacing={0.5} sx={{ justifyContent: 'flex-end' }}>
                        {book.status === 'Draft' ? (
                          <Tooltip title="Publish">
                            <Button
                              size="small"
                              onClick={() => transition.mutate({ book, kind: 'publish' })}
                            >
                              Publish
                            </Button>
                          </Tooltip>
                        ) : null}

                        {book.status === 'Catalog' ? (
                          <>
                            <Tooltip title="Send to repair">
                              <Button
                                size="small"
                                onClick={() => setLifecycle({ book, kind: 'repair' })}
                              >
                                Repair
                              </Button>
                            </Tooltip>
                            <Tooltip title="Remove from the collection">
                              <Button
                                size="small"
                                color="error"
                                onClick={() => setLifecycle({ book, kind: 'remove' })}
                              >
                                Remove
                              </Button>
                            </Tooltip>
                          </>
                        ) : null}

                        {book.status === 'Repair' ? (
                          <Tooltip title="Back on the shelves">
                            <Button
                              size="small"
                              onClick={() => transition.mutate({ book, kind: 'return' })}
                            >
                              Return
                            </Button>
                          </Tooltip>
                        ) : null}

                        {book.status === 'Deleted' ? (
                          <Tooltip title="Restore to the catalogue">
                            <Button
                              size="small"
                              onClick={() => transition.mutate({ book, kind: 'restore' })}
                            >
                              Restore
                            </Button>
                          </Tooltip>
                        ) : null}
                      </Stack>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Paper>

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

      <BookWizardDialog
        open={wizardOpen}
        onClose={() => setWizardOpen(false)}
        onSaved={setNotice}
      />

      <BookLifecycleDialog
        book={lifecycle?.book ?? null}
        kind={lifecycle?.kind ?? null}
        onClose={() => setLifecycle(null)}
        onDone={setNotice}
      />
    </Stack>
  );
};

import {
  Box,
  Button,
  Chip,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  InputAdornment,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { EmptyState, ErrorState } from '../../../shared/components/StateViews';
import { TableSkeleton } from '../../../shared/components/TableSkeleton';
import { TablePagerBar } from '../../../shared/components/TablePagerBar';
import { formatDate } from '../../membership/planCopy';
import {
  getMyReservations,
  type Reservation,
  type ReservationStatus,
} from '../api/reservationsApi';
import { DELIVERY_LABEL, STATUS_FILTERS, nextAction, statusLabel, statusTone } from '../reservationCopy';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { HandoverDialog } from '../components/HandoverDialog';
import { RateBookDialog } from '../../catalog/components/RateBookDialog';
import { ReservationsAside } from '../components/ReservationsAside';
import { useMemberDefaults } from '../../settings/memberDefaults';
import { useNavigate } from 'react-router-dom';

const PAGE_SIZE = 10;

/**
 * Book Reservations — what the member holds and what they owe back.
 *
 * The status column shows what the member reads, not what the API stores: a reservation past its due
 * date reads "Overdue · N days" while its stored status is still `Reserved`. Lateness is computed
 * server-side and arrives as a flag, so a browser clock in another zone cannot disagree with the
 * desk.
 */
export const ReservationsPage = () => {
  const [status, setStatus] = useState<ReservationStatus | 'All'>('All');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);
  const [term, setTerm] = useState('');
  const [debouncedTerm, setDebouncedTerm] = useState('');
  const preferredReturn = useMemberDefaults((state) => state.returns);

  // Debounced so typing a title is one request rather than one per keystroke.
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedTerm(term), 300);
    return () => clearTimeout(timer);
  }, [term]);
  const navigate = useNavigate();
  const [returning, setReturning] = useState<Reservation | null>(null);
  const [rating, setRating] = useState<Reservation | null>(null);

  const reservations = useQuery({
    queryKey: ['reservations', 'mine', status, debouncedTerm, page, pageSize],
    queryFn: () =>
      getMyReservations(status === 'All' ? undefined : status, page, pageSize, debouncedTerm),
  });

  return (
    <Stack spacing={3}>
      <Stack spacing={0.5}>
        <Typography variant="h4">Book Reservations</Typography>
        <Typography variant="body2" color="text.secondary">
          Everything you have borrowed, and what is still due back.
        </Typography>
      </Stack>

      <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
        {STATUS_FILTERS.map((filter) => (
          <Chip
            key={filter.value}
            label={filter.label}
            color={status === filter.value ? 'primary' : 'default'}
            variant={status === filter.value ? 'filled' : 'outlined'}
            onClick={() => {
              setStatus(filter.value);
              // A filter change invalidates the page number: staying on page 3 of a one-page result
              // shows an empty screen that reads as a failure.
              setPage(1);
            }}
          />
        ))}
      </Stack>

      {/* The prototype's `minmax(0,1fr) 320px`. The aside answers the questions the table raises —
          what this is costing, and which way the next one goes — without leaving the screen. */}
      <Box
        sx={{
          display: 'grid',
          gap: 3,
          alignItems: 'start',
          gridTemplateColumns: { xs: '1fr', lg: 'minmax(0,1fr) 320px' },
        }}
      >
        <Box sx={{ minWidth: 0 }}>
      {reservations.isLoading ? (
        <TableSkeleton rows={5} label="Loading your reservations" />
      ) : reservations.isError || !reservations.data ? (
        <ErrorState
          description="We could not load your reservations."
          onRetry={() => void reservations.refetch()}
        />
      ) : reservations.data.items.length === 0 ? (
        <EmptyState
          title={debouncedTerm ? 'No reservations match this filter' : 'Nothing here yet'}
          description={
            debouncedTerm
              ? 'Try a different title or author.'
              : 'Reserve a book from the catalogue and it will show up here.'
          }
        />
      ) : (
        <>
          <Paper variant="outlined">
            <Stack
              direction="row"
              spacing={2}
              sx={{
                px: 2.5,
                py: 2,
                alignItems: 'center',
                flexWrap: 'wrap',
                rowGap: 1,
                borderBottom: 1,
                borderColor: 'divider',
              }}
            >
              <Stack spacing={0.375} sx={{ minWidth: 0 }}>
                <Typography variant="h6">Check-in / Check-out</Typography>
                <Typography variant="caption" color="text.secondary">
                  Everything you have borrowed, and what is still due back.
                </Typography>
              </Stack>

              <TextField
                size="small"
                value={term}
                onChange={(event) => {
                  setTerm(event.target.value);
                  // A new filter invalidates the page number: staying on page three of a one-page
                  // result shows an empty table that reads as a failure.
                  setPage(1);
                }}
                placeholder="Filter by book or author"
                slotProps={{
                  input: {
                    startAdornment: (
                      <InputAdornment position="start">
                        <MaterialSymbol name="filter_list" size={18} sx={{ color: 'text.secondary' }} />
                      </InputAdornment>
                    ),
                  },
                }}
                sx={{
                  ml: 'auto',
                  flex: '1 1 220px',
                  minWidth: 200,
                  maxWidth: 360,
                  '& .MuiOutlinedInput-root': { height: 36, borderRadius: '18px' },
                }}
              />

              <Tooltip title="Reload">
                <IconButton
                  aria-label="Reload reservations"
                  onClick={() => void reservations.refetch()}
                  sx={{ width: 36, height: 36, border: 1, borderColor: 'divider', flexShrink: 0 }}
                >
                  <MaterialSymbol name="refresh" size={18} />
                </IconButton>
              </Tooltip>
            </Stack>

            <Box sx={{ overflowX: 'auto' }}>
            <Table size="small" sx={{ minWidth: 800 }}>
              <TableHead>
                <TableRow>
                  <TableCell>Book</TableCell>
                  <TableCell>Borrowed</TableCell>
                  <TableCell>Due</TableCell>
                  <TableCell>Delivery</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Action</TableCell>
                </TableRow>
              </TableHead>

              <TableBody>
                {reservations.data.items.map((reservation) => {
                  // The member's own default decides the wording — "Drop off at library" or
                  // "Courier pickup" — rather than a literal that contradicts their setting.
                  const action = nextAction(reservation, preferredReturn);

                  return (
                    <TableRow key={reservation.id} hover>
                      <TableCell>
                        <Stack spacing={0.25}>
                          <Typography variant="body2">{reservation.title}</Typography>
                          <Typography variant="caption" color="text.secondary">
                            {reservation.author}
                          </Typography>
                        </Stack>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {formatDate(reservation.borrowedOn)}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography
                          variant="body2"
                          color={reservation.isOverdue ? 'error.main' : 'text.primary'}
                        >
                          {formatDate(reservation.dueOn)}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Stack spacing={0.25}>
                          <Typography variant="body2" color="text.secondary">
                            {DELIVERY_LABEL[reservation.delivery]}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {reservation.cityName} — {reservation.libraryName}
                          </Typography>
                        </Stack>
                      </TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          color={statusTone(reservation)}
                          variant={reservation.status === 'Returned' ? 'filled' : 'outlined'}
                          label={statusLabel(reservation)}
                        />
                      </TableCell>
                      <TableCell align="right">
                        {/* The prototype shows both: `canRate` adds a Rate control *beside* the
                            row's action rather than replacing it, so a returned book can still be
                            reserved again in the same breath as it is reviewed. */}
                        <Stack
                          direction="row"
                          spacing={1}
                          sx={{ justifyContent: 'flex-end', flexWrap: 'wrap', rowGap: 1 }}
                        >
                          {/* BR-CAT-032 puts rating here and nowhere else: a member may review a
                              book once they have borrowed it and given it back, so the entry point
                              is a returned reservation, never the catalogue. */}
                          {reservation.status === 'Returned' ? (
                            <Button
                              size="small"
                              variant="outlined"
                              color="inherit"
                              startIcon={
                                <MaterialSymbol
                                  name="star_border"
                                  size={16}
                                  sx={{ color: '#E0A63C' }}
                                />
                              }
                              onClick={() => setRating(reservation)}
                              sx={{ height: 32, whiteSpace: 'nowrap' }}
                            >
                              Rate
                            </Button>
                          ) : null}

                          <Button
                            size="small"
                            variant="outlined"
                            color="inherit"
                            // A copy already with the courier offers nothing: BR-RSV-015 makes the
                            // middle state one the member cannot act on.
                            disabled={!action.enabled}
                            onClick={() =>
                              reservation.status === 'Returned'
                                ? navigate(`/catalog?book=${reservation.bookId}`)
                                : setReturning(reservation)
                            }
                            sx={{ height: 32, whiteSpace: 'nowrap' }}
                          >
                            {action.label}
                          </Button>
                        </Stack>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
            </Box>

            <TablePagerBar
              page={reservations.data.page}
              pageSize={pageSize}
              totalCount={reservations.data.totalCount}
              totalPages={reservations.data.totalPages}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
            />
          </Paper>

        </>
      )}
        </Box>

        <ReservationsAside />
      </Box>

      <HandoverDialog reservation={returning} onClose={() => setReturning(null)} />

      <RateBookDialog
        reservation={
          rating
            ? {
                bookId: rating.bookId,
                title: rating.title,
                author: rating.author,
                // The date the desk checked it in. `dueOn` would be the date it was *meant* back,
                // which is a different fact and reads as an accusation when the two differ.
                returnedOn: rating.checkedInAt ?? rating.dueOn,
              }
            : null
        }
        onClose={() => setRating(null)}
      />
    </Stack>
  );
};

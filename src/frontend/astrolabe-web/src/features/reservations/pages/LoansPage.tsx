import {
  Button,
  Chip,
  Pagination,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { formatDate } from '../../membership/planCopy';
import {
  getMyReservations,
  type Reservation,
  type ReservationStatus,
} from '../api/reservationsApi';
import { DELIVERY_LABEL, STATUS_FILTERS, nextAction, statusLabel, statusTone } from '../reservationCopy';
import { HandoverDialog } from '../components/HandoverDialog';

const PAGE_SIZE = 10;

/**
 * Book Reservations — what the member holds and what they owe back.
 *
 * The status column shows what the member reads, not what the API stores: a reservation past its due
 * date reads "Overdue · N days" while its stored status is still `Reserved`. Lateness is computed
 * server-side and arrives as a flag, so a browser clock in another zone cannot disagree with the
 * desk.
 */
export const LoansPage = () => {
  const [status, setStatus] = useState<ReservationStatus | 'All'>('All');
  const [page, setPage] = useState(1);
  const [returning, setReturning] = useState<Reservation | null>(null);

  const reservations = useQuery({
    queryKey: ['reservations', 'mine', status, page],
    queryFn: () => getMyReservations(status === 'All' ? undefined : status, page, PAGE_SIZE),
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

      {reservations.isLoading ? (
        <LoadingState label="Loading your reservations…" />
      ) : reservations.isError || !reservations.data ? (
        <ErrorState
          description="We could not load your reservations."
          onRetry={() => void reservations.refetch()}
        />
      ) : reservations.data.items.length === 0 ? (
        <EmptyState
          title="Nothing here yet"
          description="Reserve a book from the catalogue and it will show up here."
        />
      ) : (
        <>
          <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
            <Table size="small">
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
                  const action = nextAction(reservation, 'CourierPickup');

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
                        <Button
                          size="small"
                          variant="outlined"
                          // A copy already with the courier offers nothing: BR-RSV-015 makes the
                          // middle state one the member cannot act on.
                          disabled={!action.enabled || reservation.status === 'Returned'}
                          onClick={() => setReturning(reservation)}
                        >
                          {action.label}
                        </Button>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </Paper>

          {reservations.data.totalPages > 1 ? (
            <Stack sx={{ alignItems: 'center' }}>
              <Pagination
                count={reservations.data.totalPages}
                page={reservations.data.page}
                onChange={(_event, next) => setPage(next)}
                color="primary"
              />
            </Stack>
          ) : null}
        </>
      )}

      <HandoverDialog reservation={returning} onClose={() => setReturning(null)} />
    </Stack>
  );
};

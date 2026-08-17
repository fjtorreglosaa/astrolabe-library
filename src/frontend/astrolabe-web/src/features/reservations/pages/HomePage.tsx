import {
  Box,
  Button,
  Chip,
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
import { useNavigate } from 'react-router-dom';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState } from '../../../shared/components/StateViews';
import { StatCardsSkeleton } from '../../../shared/components/StatCardsSkeleton';
import { TableSkeleton } from '../../../shared/components/TableSkeleton';
import { formatDate, money } from '../../membership/planCopy';
import { getDashboard } from '../api/reservationsApi';
import { getMyFines } from '../../billing/api/billingApi';
import { getMyOrders } from '../../store/api/storeApi';
import { nextAction, statusLabel, statusTone } from '../reservationCopy';
import { useMemberDefaults } from '../../settings/memberDefaults';
import { HomeRecommendationsCard } from '../components/HomeRecommendationsCard';

/**
 * Home — the stat cards, what is due soonest, and what the member reads.
 *
 * The topics are derived from the member's own returned loans rather than from a stored profile, so
 * they cannot drift from what they actually borrowed.
 */
export const HomePage = () => {
  const navigate = useNavigate();
  const preferredReturn = useMemberDefaults((state) => state.returns);
  const dashboard = useQuery({ queryKey: ['reservations', 'dashboard'], queryFn: getDashboard });
  // Fines and purchases live in their own domains. Neither is required to render the page, so a
  // failure in either degrades one tile rather than the dashboard.
  const fines = useQuery({ queryKey: ['billing', 'fines'], queryFn: getMyFines });
  const orders = useQuery({ queryKey: ['store', 'orders', 1], queryFn: () => getMyOrders(1, 1) });

  if (dashboard.isLoading) {
    // The page's own shape, not a spinner in the middle of an empty screen: the figures sit above
    // the reservations, so that is the order they are suggested in and nothing moves when they land.
    return (
      <Stack spacing={3}>
        <StatCardsSkeleton count={4} />
        <TableSkeleton rows={4} label="Loading your dashboard" />
      </Stack>
    );
  }

  if (dashboard.isError || !dashboard.data) {
    return (
      <ErrorState
        description="We could not load your dashboard."
        onRetry={() => void dashboard.refetch()}
      />
    );
  }

  const data = dashboard.data;

  return (
    <Stack spacing={3}>
      <Stack spacing={0.5}>
        <Typography variant="h4">Home</Typography>
        <Typography variant="body2" color="text.secondary">
          What you are reading, and what is due back.
        </Typography>
      </Stack>

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(4, 1fr)' },
        }}
      >
        <StatCard
          icon="bookmarks"
          label="Reserved"
          value={String(data.activeReservations)}
          note={
            data.dueThisWeek > 0 ? `${data.dueThisWeek} due this week` : 'Nothing due this week'
          }
          tone="#0C7F70"
        />
        <StatCard
          icon="warning"
          label="Fines"
          // Money, not a count. The prototype puts the amount here because that is the figure a
          // member acts on; how many titles it came from is the note.
          value={money(fines.data?.outstandingCents ?? 0)}
          note={
            data.overdue > 0
              ? `${data.overdue} overdue title${data.overdue === 1 ? '' : 's'}`
              : 'Nothing outstanding'
          }
          tone={(fines.data?.outstandingCents ?? 0) > 0 ? '#B3261E' : '#0F7A63'}
        />
        <StatCard
          icon="shopping_bag"
          label="Purchased"
          value={String(orders.data?.totalCount ?? 0)}
          note={
            orders.data?.items[0]
              ? `Last: ${formatDate(orders.data.items[0].placedAt)}`
              : 'Nothing bought yet'
          }
          tone="#0F7A63"
        />
        <StatCard
          icon="trending_up"
          label={`Read in ${new Date().getUTCFullYear()}`}
          value={String(data.readThisYear)}
          // The prototype compares against last year. Nothing in the API carries a prior-year
          // figure, so this reports the lifetime total instead of inventing a comparison.
          note={`${data.returnedAllTime} returned all time`}
          tone="#0E5A6E"
        />
      </Box>

      {/*
        The prototype's two-column dashboard: `minmax(0,1.4fr) minmax(0,1fr)`, gap 24, aligned to
        the top. The reservations table earns the wider half because it is the thing the member came
        to check; the picks sit beside it rather than under it, where they were being pushed below
        the fold by a full-width table.

        `minmax(0, …)` rather than plain fractions — a grid track sized `1.4fr` will not shrink below
        its content, so one long book title would push the whole layout wider than the page.
      */}
      <Box
        sx={{
          display: 'grid',
          gap: 3,
          alignItems: 'start',
          gridTemplateColumns: { xs: '1fr', lg: 'minmax(0,1.4fr) minmax(0,1fr)' },
        }}
      >
        <Paper variant="outlined" sx={{ borderRadius: '12px', overflow: 'hidden' }}>
          <Stack
            direction="row"
            sx={{
              px: 2.5,
              py: 2.25,
              alignItems: 'center',
              justifyContent: 'space-between',
              borderBottom: 1,
              borderColor: 'divider',
            }}
          >
            <Typography variant="h6">Active reservations</Typography>
            <Button size="small" onClick={() => navigate('/reservations')}>
              View all
            </Button>
          </Stack>

          {data.activeSoonest.length === 0 ? (
            <EmptyState
              title="No active reservations"
              description="Reserve a book from the catalogue to see it here."
            />
          ) : (
            <Box sx={{ overflowX: 'auto' }}>
              <Table size="small" sx={{ minWidth: 520 }}>
                <TableHead>
                  {/* The prototype's dashboard subset of the reservations columns:
                      Book, Due, Status, Action. */}
                  <TableRow>
                    <TableCell>Book</TableCell>
                    <TableCell>Due</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell align="right">Action</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {/* Four, as the prototype slices it. The rest are one click away under
                      "View all", and a dashboard that reprints the whole table is not a summary. */}
                  {data.activeSoonest.slice(0, 4).map((reservation) => {
                    const action = nextAction(reservation, preferredReturn);

                    return (
                    <TableRow key={reservation.id} hover>
                      <TableCell>
                        <Typography variant="body2">{reservation.title}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {reservation.author}
                        </Typography>
                      </TableCell>
                      <TableCell sx={{ whiteSpace: 'nowrap' }}>
                        <Typography variant="body2" color="text.secondary">
                          {formatDate(reservation.dueOn)}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          color={statusTone(reservation)}
                          variant="outlined"
                          label={statusLabel(reservation)}
                        />
                      </TableCell>
                      <TableCell align="right">
                        <Button
                          size="small"
                          variant="outlined"
                          color="inherit"
                          disabled={!action.enabled}
                          onClick={() => navigate('/reservations')}
                          sx={{ height: 32, whiteSpace: 'nowrap' }}
                        >
                          {action.label}
                        </Button>
                      </TableCell>
                    </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </Box>
          )}
        </Paper>

        {/* Topics moved to the profile, where the prototype keeps them as "Preferred topics". The
            dashboard's right column is the recommendations panel and nothing else. */}
        <HomeRecommendationsCard />
      </Box>
    </Stack>
  );
};

/**
 * One dashboard tile.
 *
 * <p>
 * The prototype's layout: the label on the left, the icon on the right, the figure beneath at 34px
 * in serif, and a note under that. Ours had the icon and label side by side and the figure at body
 * scale, which made four tiles read as four sentences instead of four numbers.
 * </p>
 * <p>
 * Not a link. Every tile used to navigate to the reservations screen, including the one about fines
 * and the one about purchases — a card that takes you somewhere unrelated to what it says is worse
 * than one that does nothing.
 * </p>
 */
const StatCard = ({
  icon,
  label,
  value,
  note,
  tone,
}: {
  icon: string;
  label: string;
  value: string;
  note: string;
  tone: string;
}) => (
  <Paper variant="outlined" sx={{ p: 2.5 }}>
    <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
      <Typography variant="overline" color="text.secondary">
        {label}
      </Typography>
      <MaterialSymbol name={icon} size={20} sx={{ color: tone, flexShrink: 0 }} />
    </Stack>
    <Typography variant="h1" sx={{ mt: 1.5, fontSize: '2.125rem', lineHeight: 1 }}>
      {value}
    </Typography>
    <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.75 }}>
      {note}
    </Typography>
  </Paper>
);

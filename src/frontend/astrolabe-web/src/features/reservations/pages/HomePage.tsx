import { Box, Chip, LinearProgress, Paper, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link as RouterLink } from 'react-router-dom';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { GENRE_LABEL } from '../../catalog/catalogCopy';
import type { Genre } from '../../catalog/api/catalogApi';
import { formatDate } from '../../membership/planCopy';
import { getDashboard } from '../api/reservationsApi';
import { statusLabel, statusTone } from '../reservationCopy';

/**
 * Home — the stat cards, what is due soonest, and what the member reads.
 *
 * The topics are derived from the member's own returned loans rather than from a stored profile, so
 * they cannot drift from what they actually borrowed.
 */
export const HomePage = () => {
  const dashboard = useQuery({ queryKey: ['reservations', 'dashboard'], queryFn: getDashboard });

  if (dashboard.isLoading) {
    return <LoadingState label="Loading your dashboard…" />;
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
          value={data.activeReservations}
          note={
            data.dueThisWeek > 0
              ? `${data.dueThisWeek} due this week`
              : 'Nothing due this week'
          }
          tone="primary.main"
        />
        <StatCard
          icon="warning"
          label="Overdue"
          value={data.overdue}
          note={data.overdue > 0 ? 'Return them to stop the fine' : 'Nothing outstanding'}
          tone={data.overdue > 0 ? 'error.main' : 'success.main'}
        />
        <StatCard
          icon="library_books"
          label="Returned"
          value={data.returnedAllTime}
          note="All time"
          tone="success.main"
        />
        <StatCard
          icon="trending_up"
          label={`Read in ${new Date().getUTCFullYear()}`}
          value={data.readThisYear}
          note="Checked in by the library"
          tone="primary.main"
        />
      </Box>

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack spacing={2}>
          <Typography variant="h6">Due soonest</Typography>

          {data.activeSoonest.length === 0 ? (
            <EmptyState
              title="No active reservations"
              description="Reserve a book from the catalogue to see it here."
            />
          ) : (
            <Stack spacing={1.5}>
              {data.activeSoonest.map((reservation) => (
                <Stack
                  key={reservation.id}
                  direction="row"
                  spacing={2}
                  sx={{ alignItems: 'center', justifyContent: 'space-between' }}
                >
                  <Stack spacing={0.25} sx={{ minWidth: 0 }}>
                    <Typography variant="body2" noWrap>
                      {reservation.title}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Due {formatDate(reservation.dueOn)} · {reservation.cityName} —{' '}
                      {reservation.libraryName}
                    </Typography>
                  </Stack>
                  <Chip
                    size="small"
                    color={statusTone(reservation)}
                    variant="outlined"
                    label={statusLabel(reservation)}
                  />
                </Stack>
              ))}
            </Stack>
          )}
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack spacing={2}>
          <Stack spacing={0.5}>
            <Typography variant="h6">What you read</Typography>
            <Typography variant="body2" color="text.secondary">
              Built from the books the library has checked back in, not from a profile you filled in.
            </Typography>
          </Stack>

          {data.topics.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              Return your first book and your topics will appear here.
            </Typography>
          ) : (
            <Stack spacing={1.5}>
              {data.topics.map((topic) => (
                <Stack key={topic.genre} spacing={0.5}>
                  <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                    <Typography variant="body2">
                      {GENRE_LABEL[topic.genre as Genre] ?? topic.genre}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {topic.count} · {topic.percent}%
                    </Typography>
                  </Stack>
                  <LinearProgress variant="determinate" value={topic.percent} />
                </Stack>
              ))}
            </Stack>
          )}
        </Stack>
      </Paper>
    </Stack>
  );
};

const StatCard = ({
  icon,
  label,
  value,
  note,
  tone,
}: {
  icon: string;
  label: string;
  value: number;
  note: string;
  tone: string;
}) => (
  <Paper variant="outlined" sx={{ p: 2 }} component={RouterLink} to="/loans" style={{ textDecoration: 'none' }}>
    <Stack spacing={1}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <MaterialSymbol name={icon} size={20} sx={{ color: tone }} />
        <Typography variant="overline" color="text.secondary">
          {label}
        </Typography>
      </Stack>
      <Typography variant="h4">{value}</Typography>
      <Typography variant="caption" color="text.secondary">
        {note}
      </Typography>
    </Stack>
  </Paper>
);

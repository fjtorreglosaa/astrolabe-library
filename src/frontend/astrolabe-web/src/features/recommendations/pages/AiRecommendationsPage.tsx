import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  Chip,
  Stack,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { BookCover } from '../../catalog/components/BookCover';
import {
  getMyRecommendations,
  refreshRecommendations,
  type Recommendation,
} from '../api/recommendationsApi';
import { REFRESH_LIMIT_NOTE, SOURCE_LABEL } from '../recommendationsCopy';

/**
 * AI recommendations.
 *
 * <p>
 * The screen never decides what a member gets. Which of the two answers arrives — a personalised set
 * or the most-borrowed fallback — depends on whether a library in their city has connected a key,
 * and the browser is not told that. It renders what the server sent and shows the sentence the
 * server chose, because both depend on a rule this side cannot see.
 * </p>
 * <p>
 * There is deliberately no error state for a failed generation. BR-REC-007 makes that impossible on
 * the server: a provider failure yields the previous set or the fallback, so the only errors that
 * reach here are the ones about the member's own plan or their refresh rate.
 * </p>
 */
export const AiRecommendationsPage = () => {
  const queryClient = useQueryClient();
  const [notice, setNotice] = useState<string | null>(null);

  const recommendations = useQuery({
    queryKey: ['recommendations'],
    queryFn: getMyRecommendations,
  });

  const refresh = useMutation({
    mutationFn: refreshRecommendations,
    onSuccess: (set) => {
      queryClient.setQueryData(['recommendations'], set);
      setNotice(null);
    },
    onError: (error) => {
      const code = (error as { response?: { data?: { code?: string } } })?.response?.data?.code;

      // The one refusal worth explaining rather than showing raw: a member who pressed a button is
      // owed a reason, and "you did nothing wrong, it costs money" is the reason.
      setNotice(
        code === 'recommendations.regenerated_too_recently'
          ? REFRESH_LIMIT_NOTE
          : 'We could not refresh these just now.',
      );
    },
  });

  const set = recommendations.data;

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'flex-start' } }}
      >
        <Stack spacing={0.5}>
          <Typography variant="h4">AI recommendations</Typography>
          {set ? (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <Chip
                size="small"
                variant="outlined"
                color={set.source === 'Model' ? 'primary' : 'default'}
                icon={
                  <MaterialSymbol
                    name={set.source === 'Model' ? 'auto_awesome' : 'trending_up'}
                    size={16}
                  />
                }
                label={SOURCE_LABEL[set.source]}
              />
            </Stack>
          ) : null}
        </Stack>

        {set ? (
          <Button
            variant="outlined"
            startIcon={<MaterialSymbol name="refresh" size={20} />}
            disabled={!set.canRegenerate}
            loading={refresh.isPending}
            onClick={() => refresh.mutate()}
          >
            Refresh
          </Button>
        ) : null}
      </Stack>

      {notice ? (
        <Alert severity="info" onClose={() => setNotice(null)}>
          {notice}
        </Alert>
      ) : null}

      {recommendations.isLoading ? (
        <LoadingState label="Choosing for you…" />
      ) : recommendations.isError || !set ? (
        // Reached only when the plan excludes the surface, or the network failed. Not when a
        // provider did — the server falls back instead.
        <ErrorState
          description={
            (recommendations.error as { response?: { data?: { title?: string } } })?.response?.data
              ?.title ?? 'We could not load your recommendations.'
          }
          onRetry={() => void recommendations.refetch()}
        />
      ) : set.items.length === 0 ? (
        <EmptyState
          title="Nothing to suggest yet"
          description="Borrow a book or two and this fills up."
        />
      ) : (
        <>
          {/* The server's sentence, not ours. It explains which answer this is and why. */}
          <Typography variant="body2" color="text.secondary">
            {set.note}
          </Typography>

          <Stack spacing={2}>
            {set.items.map((item) => (
              <SuggestionCard key={item.bookId} item={item} />
            ))}
          </Stack>
        </>
      )}
    </Stack>
  );
};

const SuggestionCard = ({ item }: { item: Recommendation }) => (
  <Card variant="outlined">
    <CardActionArea sx={{ p: 2 }}>
      <Stack direction="row" spacing={2}>
        <Box sx={{ width: 56, flexShrink: 0 }}>
          <BookCover bookId={item.bookId} title={item.title} coverUrl={item.coverUrl} height={78} />
        </Box>
        <Stack spacing={0.5} sx={{ minWidth: 0, flex: 1 }}>
          <Stack
            direction="row"
            spacing={1}
            sx={{ justifyContent: 'space-between', alignItems: 'flex-start' }}
          >
            <Stack spacing={0.25} sx={{ minWidth: 0 }}>
              <Typography variant="subtitle1" noWrap>
                {item.title}
              </Typography>
              <Typography variant="body2" color="text.secondary" noWrap>
                {item.author}
              </Typography>
            </Stack>
            {/* Only when the model supplied one. The fallback has no match to report, and a 0%
                beside a perfectly good book would read as a warning. */}
            {item.matchPercent > 0 ? (
              <Chip size="small" color="primary" variant="outlined" label={`${item.matchPercent}%`} />
            ) : null}
          </Stack>

          {/* BR-REC-010. The reason is the whole difference between a recommendation and a list. */}
          <Typography variant="body2" sx={{ pt: 0.5 }}>
            {item.reason}
          </Typography>
        </Stack>
      </Stack>
    </CardActionArea>
  </Card>
);

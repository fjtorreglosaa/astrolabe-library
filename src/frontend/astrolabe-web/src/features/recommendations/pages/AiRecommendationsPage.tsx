import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  IconButton,
  Paper,
  Skeleton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ReserveDialog } from '../../reservations/components/ReserveDialog';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState } from '../../../shared/components/StateViews';
import { BookCover } from '../../catalog/components/BookCover';
import {
  getMyRecommendations,
  refreshRecommendations,
  type Recommendation,
  type RecommendationSource,
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
/** The prototype's `repeat(auto-fill, minmax(300px, 1fr))`. */
const SUGGESTION_GRID = 'repeat(auto-fill, minmax(300px, 1fr))';

export const AiRecommendationsPage = () => {
  const navigate = useNavigate();
  // Borrowing from a suggestion opens the same reservation modal the catalogue uses. Re-implementing
  // it here would be a second place for the plan and copy rules to be applied.
  const [reservingBookId, setReservingBookId] = useState<string | null>(null);
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
      {/* The prototype's "Recommendation engine" card: the mark, the explanation of which answer
          this is, and the two controls. It replaces a page heading with a button floated beside it —
          the sentence about where these came from is the point, and it needs to sit with them. */}
      <Paper variant="outlined" sx={{ p: 3 }}>
        <Stack direction="row" spacing={2.25} sx={{ alignItems: 'center', flexWrap: 'wrap', rowGap: 2 }}>
          <MaterialSymbol name="auto_awesome" size={28} sx={{ color: '#0C7F70', flexShrink: 0 }} />

          <Stack spacing={0.5} sx={{ flex: 1, minWidth: 220 }}>
            <Typography variant="h5">Recommendation engine</Typography>
            <Typography variant="body2" color="text.secondary">
              {/* The server's own sentence. It says whether a model chose these or the
                  most-borrowed fallback did, which is not something the browser can know. */}
              {set ? set.note : 'Reading your history…'}
            </Typography>
          </Stack>

          {set ? (
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
          ) : null}

          <Tooltip title="Refresh recommendations">
            <span>
              <IconButton
                aria-label="Refresh recommendations"
                disabled={!set?.canRegenerate || refresh.isPending}
                onClick={() => refresh.mutate()}
                sx={{ width: 38, height: 38, border: 1, borderColor: 'divider' }}
              >
                <MaterialSymbol name="refresh" size={18} />
              </IconButton>
            </span>
          </Tooltip>

          <Button variant="outlined" color="inherit" onClick={() => navigate('/settings')} sx={{ height: 38 }}>
            AI settings
          </Button>
        </Stack>
      </Paper>

      {notice ? (
        <Alert severity="info" onClose={() => setNotice(null)}>
          {notice}
        </Alert>
      ) : null}

      {recommendations.isLoading ? (
        <Stack spacing={2}>
          <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
            <CircularProgress size={15} thickness={6} sx={{ color: '#0C7F70' }} />
            <Typography variant="body2" color="text.secondary">
              Reading your history and building fresh picks…
            </Typography>
          </Stack>
          <Box sx={{ display: 'grid', gap: 2.25, gridTemplateColumns: SUGGESTION_GRID }}>
            {Array.from({ length: 4 }, (_, index) => (
              <Paper key={index} variant="outlined" sx={{ p: 2.25, display: 'flex', gap: 2 }}>
                <Skeleton variant="rounded" width={74} height={104} sx={{ flexShrink: 0 }} />
                <Stack spacing={1.125} sx={{ flex: 1, minWidth: 0 }}>
                  <Skeleton height={17} width="80%" />
                  <Skeleton height={11} width="50%" />
                  <Skeleton height={11} width="100%" />
                  <Skeleton height={11} width="90%" />
                </Stack>
              </Paper>
            ))}
          </Box>
        </Stack>
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
        <Box sx={{ display: 'grid', gap: 2.25, gridTemplateColumns: SUGGESTION_GRID }}>
          {set.items.map((item) => (
            <SuggestionCard
              key={item.bookId}
              item={item}
              source={set.source}
              onBorrow={setReservingBookId}
            />
          ))}
        </Box>
      )}

      <ReserveDialog
        bookId={reservingBookId}
        onClose={() => setReservingBookId(null)}
        onReserved={() => setReservingBookId(null)}
      />
    </Stack>
  );
};

/**
 * One suggestion, laid out as the prototype has it: a 74x104 cover beside the reason, with the
 * match and a Borrow control pinned to the foot of the card.
 *
 * <p>
 * Horizontal, not a poster. These are argued rather than browsed — the sentence explaining <em>why</em>
 * this book is the reason `BR-REC-010` requires one, and a vertical card gives it a column two words
 * wide.
 * </p>
 */
const SuggestionCard = ({
  item,
  source,
  onBorrow,
}: {
  item: Recommendation;
  source: RecommendationSource;
  onBorrow: (bookId: string) => void;
}) => (
  <Paper variant="outlined" sx={{ p: 2.25, display: 'flex', gap: 2 }}>
    <Box sx={{ width: 74, flexShrink: 0 }}>
      <BookCover bookId={item.bookId} title={item.title} coverUrl={item.coverUrl} height={104} />
    </Box>

    <Stack sx={{ minWidth: 0, flex: 1 }}>
      <Typography variant="h6" sx={{ lineHeight: 1.2 }}>
        {item.title}
      </Typography>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.375 }}>
        {item.author}
      </Typography>

      {/* BR-REC-010. The reason is the whole difference between a recommendation and a list. */}
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1.25, lineHeight: 1.55 }}>
        {item.reason}
      </Typography>

      <Stack direction="row" spacing={1.25} sx={{ mt: 'auto', pt: 1.5, alignItems: 'center' }}>
        {/*
          A match only exists when a model produced one — it is a figure the provider returns, not
          something this system computes. Where the fallback answered, the card says *that* instead
          of showing nothing: a chip that silently disappears reads as a broken card, while "Most
          borrowed" says plainly where the suggestion came from.

          Never a 0%. Beside a perfectly good book that reads as a warning.
        */}
        {source === 'Model' && item.matchPercent > 0 ? (
          <Chip
            size="small"
            label={`${item.matchPercent}% match`}
            sx={{ bgcolor: 'rgba(16,168,140,.12)', color: '#0C7F70' }}
          />
        ) : (
          <Chip
            size="small"
            variant="outlined"
            icon={<MaterialSymbol name="trending_up" size={14} />}
            label="Most borrowed"
          />
        )}
        <Button
          size="small"
          variant="contained"
          onClick={() => onBorrow(item.bookId)}
          sx={{ height: 30, ml: 'auto' }}
        >
          Borrow
        </Button>
      </Stack>
    </Stack>
  </Paper>
);

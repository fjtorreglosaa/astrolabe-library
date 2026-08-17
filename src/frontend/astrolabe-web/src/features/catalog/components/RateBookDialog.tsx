import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { useSnackbarStore } from '../../../shared/feedback/snackbarStore';
import { formatDate } from '../../membership/planCopy';
import { getReviews, publishReview, removeReview } from '../api/catalogApi';
import { tintFor } from '../catalogCopy';
import {
  COMMENT_LIMIT,
  COMMENT_NOTE,
  COMMENT_PLACEHOLDER,
  RATING_LABELS,
  RATING_REQUIRED,
  REMOVED_MESSAGE,
  UPDATED_MESSAGE,
  publishedMessage,
  returnedIntro,
} from '../reviewCopy';

/**
 * Rating a book, opened from a returned reservation.
 *
 * <p>
 * <b>Only from there.</b> `BR-CAT-032` — and the prototype's `canRate: done && !isLibrarian` — allow
 * a review once a member has borrowed the book and given it back, which is why this dialog's first
 * line names the date it came back. There is no path to it from the catalogue, so the restriction
 * never has to be delivered as a refusal: by the time anybody sees this, they have already met it.
 * </p>
 * <p>
 * The server enforces the same rule. This is the pleasant way to arrive at it, not the boundary.
 * </p>
 */
export interface RateBookDialogProps {
  /** The returned reservation being rated, or null when the dialog is closed. */
  reservation: { bookId: string; title: string; author: string; returnedOn: string } | null;
  onClose: () => void;
}

export const RateBookDialog = ({ reservation, onClose }: RateBookDialogProps) => {
  const queryClient = useQueryClient();
  const push = useSnackbarStore((state) => state.push);

  const [stars, setStars] = useState(0);
  const [comment, setComment] = useState('');

  const bookId = reservation?.bookId ?? null;

  const reviews = useQuery({
    queryKey: ['catalog', 'reviews', bookId],
    queryFn: () => getReviews(bookId!),
    enabled: bookId !== null,
  });

  const mine = reviews.data?.items.find((review) => review.isMine) ?? null;

  // Seeded from the existing review each time the dialog opens on a different book, so "Edit review"
  // starts from what the member actually wrote rather than from an empty form.
  useEffect(() => {
    setStars(mine?.rating ?? 0);
    setComment(mine?.comment ?? '');
  }, [mine, bookId]);

  const refresh = async () => {
    // The book too: a published review moves the average, and a stale star beside a fresh review is
    // a disagreement a reader notices immediately.
    await queryClient.invalidateQueries({ queryKey: ['catalog'] });
  };

  const save = useMutation({
    meta: { success: mine ? UPDATED_MESSAGE : publishedMessage(reservation?.title ?? '') },
    mutationFn: () => publishReview(bookId!, stars, comment.trim() || null),
    onSuccess: async () => {
      await refresh();
      onClose();
    },
  });

  const remove = useMutation({
    meta: { success: REMOVED_MESSAGE },
    mutationFn: () => removeReview(bookId!),
    onSuccess: async () => {
      await refresh();
      onClose();
    },
  });

  const busy = save.isPending || remove.isPending;

  if (!reservation) {
    return null;
  }

  return (
    <Dialog open onClose={busy ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.75, pb: 2 }}>
        {/* The tint block, at the prototype's 40×56. A cover would be better still, but this
            dialog is reached from a table row that does not carry one. */}
        <Box
          aria-hidden
          sx={{
            width: 40,
            height: 56,
            flexShrink: 0,
            borderRadius: '4px',
            bgcolor: tintFor(reservation.bookId),
          }}
        />
        <Stack spacing={0.5} sx={{ flex: 1, minWidth: 0 }}>
          <Typography variant="h5">{mine ? 'Edit your review' : 'How was this book?'}</Typography>
          <Typography variant="body2" color="text.secondary">
            {reservation.title} · {reservation.author}
          </Typography>
        </Stack>
        <IconButton aria-label="Close" onClick={onClose} disabled={busy} sx={{ mt: -0.5 }}>
          <MaterialSymbol name="close" size={19} />
        </IconButton>
      </DialogTitle>

      <DialogContent dividers>
        <Stack spacing={2.5}>
          <Typography variant="body2" color="text.secondary">
            {returnedIntro(formatDate(reservation.returnedOn))}
          </Typography>

          <Stack spacing={1}>
            <Typography variant="overline" color="text.secondary">
              Your rating
            </Typography>
            <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
              <Stack direction="row">
                {[1, 2, 3, 4, 5].map((value) => (
                  <IconButton
                    key={value}
                    onClick={() => setStars(value)}
                    // Each star says its own value. A row of five identical "star" labels tells a
                    // screen reader nothing about which one it is on.
                    aria-label={`${value} of 5 stars`}
                    aria-pressed={stars === value}
                    sx={{ width: 44, height: 44 }}
                  >
                    <MaterialSymbol
                      name={value <= stars ? 'star' : 'star_border'}
                      size={28}
                      sx={{ color: value <= stars ? '#E0A63C' : 'text.disabled' }}
                    />
                  </IconButton>
                ))}
              </Stack>
              <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 600 }}>
                {RATING_LABELS[stars] ?? RATING_LABELS[0]}
              </Typography>
            </Stack>
          </Stack>

          <Stack spacing={1}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline' }}>
              <Typography variant="overline" color="text.secondary">
                Your comment
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {comment.length} / {COMMENT_LIMIT}
              </Typography>
            </Stack>
            <TextField
              multiline
              minRows={4}
              fullWidth
              placeholder={COMMENT_PLACEHOLDER}
              value={comment}
              // Truncated as it is typed, the way the prototype does, rather than refused on submit.
              // A limit somebody discovers only after writing past it is a limit that wasted a
              // paragraph.
              onChange={(event) => setComment(event.target.value.slice(0, COMMENT_LIMIT))}
              helperText={COMMENT_NOTE}
            />
          </Stack>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        {mine ? (
          <Button color="error" onClick={() => remove.mutate()} loading={remove.isPending}>
            Remove review
          </Button>
        ) : null}
        <Box sx={{ flex: 1 }} />
        <Button color="inherit" onClick={onClose} disabled={busy}>
          Cancel
        </Button>
        <Button
          variant="contained"
          loading={save.isPending}
          onClick={() => {
            // The prototype's own guard and its own words. Checked before the request rather than
            // after, because a rating of zero is not something the server should have to name.
            if (stars === 0) {
              push({ title: RATING_REQUIRED, tone: 'error' });
              return;
            }

            save.mutate();
          }}
        >
          {mine ? 'Save review' : 'Publish review'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

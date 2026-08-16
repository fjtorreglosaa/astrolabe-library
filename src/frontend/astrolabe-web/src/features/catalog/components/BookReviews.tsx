import {
  Alert,
  Avatar,
  Button,
  Divider,
  Rating,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { formatDate } from '../../membership/planCopy';
import { getReviews, publishReview, removeReview, type Review } from '../api/catalogApi';

/**
 * A book's reviews, and the member's own.
 *
 * <p>
 * One review per member per book, so this is a single form that writes or rewrites rather than a
 * thread. The server decides which review is theirs — `isMine` arrives on the DTO — because the
 * browser knowing its own member identifier is not the same as the server agreeing.
 * </p>
 * <p>
 * The rating and the comment are one act. A star with no words is a complete review and the comment
 * stays optional; words with no star are not, because the average that feeds the catalogue is built
 * from stars alone.
 * </p>
 */
export const BookReviews = ({ bookId }: { bookId: string }) => {
  const queryClient = useQueryClient();
  const [rating, setRating] = useState<number | null>(null);
  const [comment, setComment] = useState('');
  const [editing, setEditing] = useState(false);
  const [confirmingRemoval, setConfirmingRemoval] = useState(false);

  const reviews = useQuery({
    queryKey: ['catalog', 'reviews', bookId],
    queryFn: () => getReviews(bookId),
  });

  const mine = reviews.data?.items.find((review) => review.isMine) ?? null;
  const others = reviews.data?.items.filter((review) => !review.isMine) ?? [];

  const refresh = async () => {
    // The book too: publishing a review moves its average, and a stale star beside a fresh review
    // is the kind of disagreement a reader notices immediately.
    await queryClient.invalidateQueries({ queryKey: ['catalog'] });
  };

  const publish = useMutation({
    mutationFn: () => publishReview(bookId, rating ?? 0, comment.trim() || null),
    onSuccess: async () => {
      setEditing(false);
      await refresh();
    },
  });

  const remove = useMutation({
    mutationFn: () => removeReview(bookId),
    onSuccess: async () => {
      setConfirmingRemoval(false);
      setEditing(false);
      setRating(null);
      setComment('');
      await refresh();
    },
  });

  const startEditing = () => {
    setRating(mine?.rating ?? null);
    setComment(mine?.comment ?? '');
    setEditing(true);
  };

  return (
    <Stack spacing={2}>
      <Divider />

      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <MaterialSymbol name="reviews" size={20} sx={{ color: 'text.secondary' }} />
        <Typography variant="subtitle2">
          {reviews.data?.totalCount
            ? `${reviews.data.totalCount} ${reviews.data.totalCount === 1 ? 'review' : 'reviews'}`
            : 'No reviews yet'}
        </Typography>
      </Stack>

      {editing || !mine ? (
        <Stack spacing={1}>
          <Typography variant="body2" color="text.secondary">
            {mine ? 'Edit your review' : 'Read it? Say what you thought.'}
          </Typography>
          <Rating value={rating} onChange={(_event, value) => setRating(value)} />
          <TextField
            size="small"
            multiline
            minRows={2}
            placeholder="Your comment (optional)"
            value={comment}
            onChange={(event) => setComment(event.target.value)}
            fullWidth
          />
          {publish.isError ? (
            <Alert severity="error">
              {(publish.error as { response?: { data?: { title?: string } } })?.response?.data
                ?.title ?? 'We could not save that review.'}
            </Alert>
          ) : null}
          <Stack direction="row" spacing={1}>
            <Button
              size="small"
              variant="contained"
              disabled={!rating}
              loading={publish.isPending}
              onClick={() => publish.mutate()}
            >
              {mine ? 'Save changes' : 'Publish review'}
            </Button>
            {editing ? (
              <Button size="small" color="inherit" onClick={() => setEditing(false)}>
                Cancel
              </Button>
            ) : null}
          </Stack>
        </Stack>
      ) : (
        <Stack spacing={1}>
          <ReviewRow review={mine} />
          <Stack direction="row" spacing={1}>
            <Button size="small" onClick={startEditing}>
              Edit
            </Button>
            <Button size="small" color="error" onClick={() => setConfirmingRemoval(true)}>
              Remove review
            </Button>
          </Stack>
        </Stack>
      )}

      {others.length > 0 ? (
        <Stack spacing={1.5}>
          <Divider />
          {others.map((review) => (
            <ReviewRow key={review.id} review={review} />
          ))}
        </Stack>
      ) : null}

      <ConfirmDialog
        open={confirmingRemoval}
        title="Remove your review?"
        description="It disappears from the book and stops counting toward its rating. You can write another one later."
        confirmLabel="Yes, remove"
        destructive
        busy={remove.isPending}
        onConfirm={() => remove.mutate()}
        onCancel={() => setConfirmingRemoval(false)}
      />
    </Stack>
  );
};

const ReviewRow = ({ review }: { review: Review }) => (
  <Stack direction="row" spacing={1.5}>
    <Avatar sx={{ width: 32, height: 32, fontSize: 13, bgcolor: 'primary.main' }}>
      {review.initials}
    </Avatar>
    <Stack spacing={0.25} sx={{ minWidth: 0 }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <Typography variant="body2" sx={{ fontWeight: 600 }}>
          {review.memberName}
        </Typography>
        <Rating value={review.rating} readOnly size="small" />
      </Stack>
      <Typography variant="caption" color="text.secondary">
        {formatDate(review.createdAt)}
        {/* Shown rather than hidden: a review that was rewritten after the fact is a different
            thing from one written once, and the reader is entitled to tell them apart. */}
        {review.editedAt ? ' · edited' : ''}
      </Typography>
      {review.comment ? <Typography variant="body2">{review.comment}</Typography> : null}
    </Stack>
  </Stack>
);

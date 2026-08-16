import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  Rating,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { ErrorState, LoadingState } from '../../../shared/components/StateViews';
import type { Membership } from '../../membership/api/membershipApi';
import { money } from '../../membership/planCopy';
import { getBook, getReviews, publishReview, removeReview } from '../api/catalogApi';
import { GENRE_LABEL, bookBadgeLabel, copyReasonLabel } from '../catalogCopy';
import { BookCover } from './BookCover';

/**
 * The book detail panel: metadata, every branch that holds it, and the reviews.
 *
 * Opens for any book, including one the member cannot reserve — BR-CAT-016. The per-branch list is
 * the reason the panel exists: the card can only show one badge, and a member refused for reach
 * needs to see which branch does hold the book.
 */
export interface BookDetailDialogProps {
  bookId: string | null;
  membership: Membership | undefined;
  onClose: () => void;
}

export const BookDetailDialog = ({ bookId, membership, onClose }: BookDetailDialogProps) => {
  const queryClient = useQueryClient();
  const [rating, setRating] = useState<number | null>(null);
  const [comment, setComment] = useState('');

  const book = useQuery({
    queryKey: ['catalog', 'book', bookId],
    queryFn: () => getBook(bookId!),
    enabled: bookId !== null,
  });

  const reviews = useQuery({
    queryKey: ['catalog', 'reviews', bookId],
    queryFn: () => getReviews(bookId!),
    enabled: bookId !== null,
  });

  const mine = reviews.data?.items.find((review) => review.isMine);

  // The form starts from the member's existing review, so reviewing twice reads as an edit rather
  // than as writing a second one — which is what BR-CAT-027 actually does.
  useEffect(() => {
    setRating(mine?.rating ?? null);
    setComment(mine?.comment ?? '');
  }, [mine?.id, mine?.rating, mine?.comment]);

  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['catalog', 'book', bookId] }),
      queryClient.invalidateQueries({ queryKey: ['catalog', 'reviews', bookId] }),
      // The listing carries the rating too, so leaving it stale would show two different scores
      // for one book on the same screen.
      queryClient.invalidateQueries({ queryKey: ['catalog', 'books'] }),
    ]);
  };

  const saveReview = useMutation({
    mutationFn: () => publishReview(bookId!, rating ?? 0, comment.trim() || null),
    onSuccess: refresh,
  });

  const deleteReview = useMutation({
    mutationFn: () => removeReview(bookId!),
    onSuccess: refresh,
  });

  return (
    <Dialog open={bookId !== null} onClose={onClose} maxWidth="md" fullWidth>
      {book.isLoading ? (
        <DialogContent>
          <LoadingState label="Loading the book…" />
        </DialogContent>
      ) : book.isError || !book.data ? (
        <DialogContent>
          <ErrorState description="We could not load that book." onRetry={() => void book.refetch()} />
        </DialogContent>
      ) : (
        <>
          <DialogTitle sx={{ pb: 1 }}>
            <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start' }}>
              <Stack spacing={0.5} sx={{ flex: 1, minWidth: 0 }}>
                <Typography variant="h6">{book.data.title}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {book.data.author}
                </Typography>
              </Stack>
              <IconButton onClick={onClose} size="small" aria-label="Close">
                <MaterialSymbol name="close" size={20} />
              </IconButton>
            </Stack>
          </DialogTitle>

          <DialogContent>
            <Stack spacing={3}>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <Box sx={{ width: { xs: '100%', sm: 160 }, flexShrink: 0 }}>
                  <BookCover
                    bookId={book.data.id}
                    title={book.data.title}
                    coverUrl={book.data.coverUrl}
                    height={220}
                  />
                </Box>

                <Stack spacing={1.5} sx={{ flex: 1 }}>
                  <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
                    <Chip size="small" variant="outlined" label={GENRE_LABEL[book.data.genre]} />
                    <Chip size="small" variant="outlined" label={`${book.data.tier} plan`} />
                    <Chip size="small" variant="outlined" label={money(book.data.retailPriceCents)} />
                  </Stack>

                  <Stack spacing={0.25}>
                    <Detail label="ISBN" value={book.data.isbn} />
                    {book.data.publisher ? (
                      <Detail label="Publisher" value={book.data.publisher} />
                    ) : null}
                    <Detail
                      label="Rating"
                      value={
                        book.data.averageRating === null
                          ? 'No reviews yet'
                          : `${book.data.averageRating.toFixed(1)} from ${book.data.reviewCount} review${book.data.reviewCount === 1 ? '' : 's'}`
                      }
                    />
                  </Stack>

                  {book.data.badge ? (
                    <Alert severity="warning" icon={<MaterialSymbol name="info" size={20} />}>
                      {bookBadgeLabel(book.data.badge, membership)}
                    </Alert>
                  ) : null}

                  <Button
                    variant="contained"
                    disabled={!book.data.canReserve}
                    sx={{ alignSelf: 'flex-start' }}
                  >
                    {book.data.canReserve ? 'Reserve' : 'Unavailable'}
                  </Button>
                </Stack>
              </Stack>

              <Divider />

              <Stack spacing={1}>
                <Typography variant="subtitle2">Where to find it</Typography>

                {book.data.copies.map((copy) => (
                  <Stack
                    key={copy.libraryId}
                    direction="row"
                    spacing={2}
                    sx={{ alignItems: 'center', justifyContent: 'space-between' }}
                  >
                    <Stack spacing={0.25}>
                      <Typography variant="body2">
                        {copy.cityName} — {copy.libraryName}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {copy.availableCount} / {copy.totalCount} available
                      </Typography>
                    </Stack>

                    {copy.canReserve ? (
                      <Chip size="small" color="success" label="Reservable" />
                    ) : copy.reason ? (
                      <Chip
                        size="small"
                        variant="outlined"
                        label={copyReasonLabel(copy.reason, copy, membership)}
                      />
                    ) : null}
                  </Stack>
                ))}
              </Stack>

              <Divider />

              <Stack spacing={2}>
                <Typography variant="subtitle2">{mine ? 'Your review' : 'Write a review'}</Typography>

                <Stack spacing={1.5}>
                  <Rating value={rating} onChange={(_event, value) => setRating(value)} />
                  <TextField
                    multiline
                    minRows={2}
                    fullWidth
                    size="small"
                    placeholder="What did you think of it?"
                    value={comment}
                    onChange={(event) => setComment(event.target.value)}
                  />
                  <Stack direction="row" spacing={1}>
                    <Button
                      variant="contained"
                      size="small"
                      // A rating is required; a comment is not. Sending a zero would be refused by
                      // the API, so the button stays disabled rather than inviting the error.
                      disabled={!rating}
                      loading={saveReview.isPending}
                      onClick={() => saveReview.mutate()}
                    >
                      {mine ? 'Update review' : 'Publish review'}
                    </Button>
                    {mine ? (
                      <Button
                        size="small"
                        color="error"
                        loading={deleteReview.isPending}
                        onClick={() => deleteReview.mutate()}
                      >
                        Remove
                      </Button>
                    ) : null}
                  </Stack>
                </Stack>

                {reviews.data && reviews.data.totalCount > 0 ? (
                  <Stack spacing={1.5}>
                    <Divider />
                    {reviews.data.items.map((review) => (
                      <Stack key={review.id} direction="row" spacing={1.5}>
                        <Box
                          sx={{
                            width: 32,
                            height: 32,
                            borderRadius: '50%',
                            bgcolor: 'action.selected',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            flexShrink: 0,
                          }}
                        >
                          <Typography variant="caption">{review.initials}</Typography>
                        </Box>
                        <Stack spacing={0.25} sx={{ flex: 1 }}>
                          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                            <Typography variant="body2">{review.memberName}</Typography>
                            <Rating value={review.rating} readOnly size="small" />
                          </Stack>
                          {review.comment ? (
                            <Typography variant="body2" color="text.secondary">
                              {review.comment}
                            </Typography>
                          ) : null}
                        </Stack>
                      </Stack>
                    ))}
                  </Stack>
                ) : null}
              </Stack>
            </Stack>
          </DialogContent>
        </>
      )}
    </Dialog>
  );
};

const Detail = ({ label, value }: { label: string; value: string }) => (
  <Stack direction="row" spacing={1}>
    <Typography variant="caption" color="text.secondary" sx={{ minWidth: 72 }}>
      {label}
    </Typography>
    <Typography variant="caption">{value}</Typography>
  </Stack>
);

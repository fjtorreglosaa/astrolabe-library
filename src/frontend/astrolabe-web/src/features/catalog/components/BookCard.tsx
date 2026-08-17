import { Box, Button, Card, Chip, Stack, Typography } from '@mui/material';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { useSnackbarStore } from '../../../shared/feedback/snackbarStore';
import type { Membership } from '../../membership/api/membershipApi';
import { money } from '../../membership/planCopy';
import type { BookSummary } from '../api/catalogApi';
import { GENRE_LABEL, availabilityLabel, bookBadgeLabel, tintFor } from '../catalogCopy';

/**
 * One book in the card view.
 *
 * <p>
 * The prototype puts the <b>title on the artwork</b>, in white over a 3:4 panel, with the genre and
 * plan chips above it — so a grid reads as a shelf of covers rather than as a table with pictures.
 * The facts sit underneath: how many copies are left, the rating, the price, and the reason it
 * cannot be borrowed if that applies.
 * </p>
 * <p>
 * A book the member cannot reserve is still shown and still opens — `BR-CAT-016` makes reach
 * restrict borrowing, not discovery. What changes is the button and the badge, so the refusal is
 * legible without hiding the book.
 * </p>
 */
export interface BookCardProps {
  book: BookSummary;
  membership: Membership | undefined;
  /** Opens the detail panel. */
  onOpen: (book: BookSummary) => void;
  /** Starts a reservation directly, skipping the panel. */
  onReserve: (book: BookSummary) => void;
}

export const BookCard = ({ book, membership, onOpen, onReserve }: BookCardProps) => {
  const push = useSnackbarStore((state) => state.push);

  const badge = book.badge ? bookBadgeLabel(book.badge, membership) : null;

  return (
    <Card
      variant="outlined"
      sx={{ display: 'flex', flexDirection: 'column', height: '100%', borderRadius: '12px' }}
    >
      {/* The cover panel. Fixed at 3:4 so a grid of them lines up whether or not each book has
          artwork — a card that sized itself to its image would make every row a different height. */}
      <Box
        onClick={() => onOpen(book)}
        role="button"
        tabIndex={0}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            onOpen(book);
          }
        }}
        aria-label={`${book.title} by ${book.author}`}
        sx={{
          position: 'relative',
          aspectRatio: '3 / 4',
          p: 2.25,
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
          overflow: 'hidden',
          cursor: 'pointer',
          bgcolor: tintFor(book.id),
          backgroundImage: book.coverUrl ? `url("${book.coverUrl}")` : undefined,
          backgroundSize: 'cover',
          backgroundPosition: 'center',
        }}
      >
        {book.coverUrl ? (
          // Only over real artwork. On a flat tint the text already has its contrast, and a second
          // wash would only muddy the colour that identifies the book.
          <Box
            aria-hidden
            sx={{
              position: 'absolute',
              inset: 0,
              background:
                'linear-gradient(180deg,rgba(4,20,26,.45) 0%,rgba(4,20,26,.10) 40%,rgba(4,20,26,.78) 100%)',
            }}
          />
        ) : null}

        <Stack direction="row" spacing={0.75} sx={{ position: 'relative', flexWrap: 'wrap', gap: 0.75 }}>
          <Chip
            size="small"
            label={GENRE_LABEL[book.genre]}
            sx={{ bgcolor: 'rgba(255,255,255,.85)', color: '#10262E', fontWeight: 600 }}
          />
          <Chip
            size="small"
            label={book.tier}
            sx={{
              bgcolor: 'rgba(255,255,255,.18)',
              border: '1px solid rgba(255,255,255,.5)',
              color: '#fff',
              fontWeight: 600,
            }}
          />
        </Stack>

        <Box sx={{ position: 'relative' }}>
          <Typography variant="h4" sx={{ color: '#fff', lineHeight: 1.15 }}>
            {book.title}
          </Typography>
          <Typography variant="caption" sx={{ display: 'block', color: 'rgba(255,255,255,.82)', mt: 0.625 }}>
            {book.author}
          </Typography>
        </Box>
      </Box>

      <Stack spacing={1.25} sx={{ p: 1.75, flex: 1 }}>
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
          <Typography variant="caption" color="text.secondary">
            {availabilityLabel(book.availableCount)}
          </Typography>

          {/* BR-CAT-030: no reviews shows no rating rather than a zero, which would read as
              unanimous dislike. */}
          {book.averageRating !== null ? (
            <Stack direction="row" spacing={0.375} sx={{ alignItems: 'center' }}>
              <MaterialSymbol name="star" size={14} fill={1} sx={{ color: '#E0A63C' }} />
              <Typography variant="caption">{book.averageRating.toFixed(1)}</Typography>
            </Stack>
          ) : null}
        </Stack>

        <Typography variant="h6">{money(book.retailPriceCents)}</Typography>

        {badge ? (
          // The prototype's locked strip. A disabled button alone says "no"; this says why, which is
          // the difference between a member thinking the app is broken and knowing what to change.
          <Stack
            direction="row"
            spacing={0.75}
            sx={{
              alignItems: 'center',
              px: 1.25,
              py: 0.75,
              borderRadius: '8px',
              bgcolor: 'rgba(179,38,30,.10)',
              color: '#B3261E',
            }}
          >
            <MaterialSymbol name="lock" size={15} />
            <Typography variant="caption" sx={{ fontWeight: 600, lineHeight: 1.3 }}>
              {badge}
            </Typography>
          </Stack>
        ) : null}

        <Stack direction="row" spacing={1} sx={{ mt: 'auto' }}>
          <Button
            size="small"
            variant={book.canReserve ? 'contained' : 'outlined'}
            color={book.canReserve ? 'primary' : 'inherit'}
            onClick={() => {
              // Not disabled. The prototype answers the tap with the reason, and a control that
              // simply goes dead leaves somebody guessing which of several rules stopped them.
              if (!book.canReserve) {
                push({
                  title: badge ?? 'This one cannot be reserved right now.',
                  body: `“${book.title}” cannot be reserved on your current plan.`,
                  tone: 'error',
                });
                return;
              }

              onReserve(book);
            }}
            sx={{ flex: 1, height: 34, borderRadius: '17px' }}
          >
            {book.canReserve ? 'Reserve' : 'Unavailable'}
          </Button>

          <Button
            size="small"
            variant="outlined"
            color="inherit"
            onClick={() => onOpen(book)}
            sx={{ height: 34, borderRadius: '17px' }}
          >
            Details
          </Button>
        </Stack>
      </Stack>
    </Card>
  );
};

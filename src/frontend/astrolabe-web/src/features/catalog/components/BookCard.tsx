import { Box, Button, Card, CardActionArea, Chip, Stack, Typography } from '@mui/material';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import type { Membership } from '../../membership/api/membershipApi';
import { money } from '../../membership/planCopy';
import type { BookSummary } from '../api/catalogApi';
import { GENRE_LABEL, availabilityLabel, bookBadgeLabel } from '../catalogCopy';
import { BookCover } from './BookCover';

/**
 * One book in the card view.
 *
 * A book the member cannot reserve is still shown and still opens — BR-CAT-016 makes reach restrict
 * borrowing, not discovery. What changes is the button and the badge, so the refusal is legible
 * without hiding the book.
 */
export interface BookCardProps {
  book: BookSummary;
  membership: Membership | undefined;
  onOpen: (book: BookSummary) => void;
}

export const BookCard = ({ book, membership, onOpen }: BookCardProps) => (
  <Card variant="outlined" sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
    <CardActionArea onClick={() => onOpen(book)} sx={{ p: 1.5, alignItems: 'stretch' }}>
      <Stack spacing={1.5}>
        <Box sx={{ position: 'relative' }}>
          <BookCover bookId={book.id} title={book.title} coverUrl={book.coverUrl} />

          {/* The tier sits on the cover, as the prototype places it: it is the first thing that
              decides whether the book is reachable at all. */}
          <Chip
            size="small"
            label={book.tier}
            sx={{ position: 'absolute', top: 8, left: 8, bgcolor: 'background.paper' }}
          />

          {book.badge ? (
            <Chip
              size="small"
              color="warning"
              label={bookBadgeLabel(book.badge, membership)}
              sx={{ position: 'absolute', bottom: 8, left: 8, maxWidth: 'calc(100% - 16px)' }}
            />
          ) : null}
        </Box>

        <Stack spacing={0.25}>
          <Typography variant="subtitle2" noWrap title={book.title}>
            {book.title}
          </Typography>
          <Typography variant="caption" color="text.secondary" noWrap>
            {book.author}
          </Typography>
        </Stack>

        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
          <Typography variant="caption" color="text.secondary">
            {GENRE_LABEL[book.genre]}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            ·
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {availabilityLabel(book.availableCount)}
          </Typography>

          {/* BR-CAT-030: a book with no reviews shows no rating rather than a zero, which would
              read as unanimous dislike. */}
          {book.averageRating !== null ? (
            <Stack direction="row" spacing={0.25} sx={{ alignItems: 'center', ml: 'auto' }}>
              <MaterialSymbol name="star" size={14} fill={1} sx={{ color: 'warning.main' }} />
              <Typography variant="caption">{book.averageRating.toFixed(1)}</Typography>
            </Stack>
          ) : null}
        </Stack>
      </Stack>
    </CardActionArea>

    <Stack
      direction="row"
      spacing={1}
      sx={{ p: 1.5, pt: 0, mt: 'auto', alignItems: 'center', justifyContent: 'space-between' }}
    >
      <Typography variant="subtitle2">{money(book.retailPriceCents)}</Typography>
      <Button
        size="small"
        variant={book.canReserve ? 'contained' : 'outlined'}
        disabled={!book.canReserve}
        onClick={() => onOpen(book)}
      >
        {book.canReserve ? 'Reserve' : 'Unavailable'}
      </Button>
    </Stack>
  </Card>
);

import { Box, Typography } from '@mui/material';
import { coverInitials, tintFor } from '../catalogCopy';

/**
 * A book's cover, or a deterministic tinted placeholder when it has none.
 *
 * The tint is derived from the identifier rather than picked at random, which is what BR-CAT-005
 * requires: a book that changed colour between two visits would be unrecognisable in a grid.
 */
export interface BookCoverProps {
  bookId: string;
  title: string;
  coverUrl: string | null;
  height?: number;
  width?: number;
}

export const BookCover = ({ bookId, title, coverUrl, height = 180, width }: BookCoverProps) => (
  <Box
    sx={{
      height,
      width: width ?? '100%',
      flexShrink: 0,
      borderRadius: 1.5,
      overflow: 'hidden',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      bgcolor: coverUrl ? 'transparent' : tintFor(bookId),
      backgroundImage: coverUrl ? `url("${coverUrl}")` : undefined,
      backgroundSize: 'cover',
      backgroundPosition: 'center',
    }}
  >
    {coverUrl ? null : (
      <Typography
        variant="h4"
        sx={{ color: 'common.white', opacity: 0.9, letterSpacing: '.08em' }}
      >
        {coverInitials(title)}
      </Typography>
    )}
  </Box>
);

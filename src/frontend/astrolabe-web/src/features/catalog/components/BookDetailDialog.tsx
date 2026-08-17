import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogContent,
  IconButton,
  Stack,
  Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { ErrorState, LoadingState } from '../../../shared/components/StateViews';
import type { Membership } from '../../membership/api/membershipApi';
import { money } from '../../membership/planCopy';
import { getBook, type CopyAvailability } from '../api/catalogApi';
import { GENRE_LABEL, bookBadgeLabel, copyReasonLabel, tintFor } from '../catalogCopy';

/**
 * The book panel: the cover, what the catalogue knows, and every branch that holds a copy.
 *
 * <p>
 * Two columns at 880px, as the prototype has it — a tinted cover panel carrying the title, and a
 * scrolling column of facts beside it. Opens for any book, including one the member cannot reserve
 * (`BR-CAT-016`): the card can only show one badge, and somebody refused for reach needs to see
 * which branch does hold it.
 * </p>
 * <p>
 * <b>There is no review form here, and no list of reviews.</b> `BR-CAT-032` allows a review once a
 * member has borrowed the book and given it back, so the only way in is a returned reservation —
 * and the prototype's own panel shows the average and nothing else. A form on this screen was a
 * control the server refuses, offered to somebody who has not read the book yet.
 * </p>
 */
export interface BookDetailDialogProps {
  bookId: string | null;
  membership: Membership | undefined;
  onClose: () => void;
  /**
   * Starts a reservation. The panel closes first — the prototype sets `detailId:null` in the same
   * breath as it opens the reservation, so the member is not left with two stacked modals and a
   * dimmed book behind the one they are filling in.
   */
  onReserve: (bookId: string) => void;
  onBuy: (bookId: string) => void;
}

export const BookDetailDialog = ({
  bookId,
  membership,
  onClose,
  onReserve,
  onBuy,
}: BookDetailDialogProps) => {
  const book = useQuery({
    queryKey: ['catalog', 'book', bookId],
    queryFn: () => getBook(bookId!),
    enabled: bookId !== null,
  });

  const data = book.data;

  const onShelf = data?.copies.reduce((total, copy) => total + copy.availableCount, 0) ?? 0;
  const total = data?.copies.reduce((sum, copy) => sum + copy.totalCount, 0) ?? 0;

  return (
    <Dialog
      open={bookId !== null}
      onClose={onClose}
      maxWidth={false}
      slotProps={{ paper: { sx: { width: '100%', maxWidth: 880, borderRadius: '14px' } } }}
    >
      {book.isLoading ? (
        <DialogContent>
          <LoadingState label="Loading the book…" />
        </DialogContent>
      ) : book.isError || !data ? (
        <DialogContent>
          <ErrorState description="We could not load that book." onRetry={() => void book.refetch()} />
        </DialogContent>
      ) : (
        <>
          <Box
            sx={{
              display: 'grid',
              // The prototype's `250px minmax(0,1fr)`. It collapses to one column on a narrow
              // screen, where a 250px art panel would leave the facts in a gutter.
              gridTemplateColumns: { xs: '1fr', sm: '250px minmax(0,1fr)' },
              maxHeight: 'calc(100vh - 64px)',
            }}
          >
            {/* The cover, as a full-bleed panel rather than a thumbnail. The title sits on it in
                white, which is why the gradient is there: a pale cover would otherwise swallow it. */}
            <Box
              sx={{
                position: 'relative',
                minHeight: { xs: 200, sm: 'auto' },
                p: 3.5,
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'flex-end',
                gap: 1.75,
                bgcolor: tintFor(data.id),
                backgroundImage: data.coverUrl ? `url("${data.coverUrl}")` : undefined,
                backgroundSize: 'cover',
                backgroundPosition: 'center',
              }}
            >
              {data.coverUrl ? (
                <Box
                  aria-hidden
                  sx={{
                    position: 'absolute',
                    inset: 0,
                    background:
                      'linear-gradient(180deg,rgba(4,20,26,.40) 0%,rgba(4,20,26,.12) 35%,rgba(4,20,26,.82) 100%)',
                  }}
                />
              ) : null}

              <Box sx={{ position: 'relative' }}>
                <Typography variant="h3" sx={{ color: '#fff', lineHeight: 1.12 }}>
                  {data.title}
                </Typography>
                <Typography variant="body2" sx={{ color: 'rgba(255,255,255,.85)', mt: 0.75 }}>
                  {data.author}
                </Typography>
              </Box>

              <Box
                sx={{
                  position: 'relative',
                  px: 1.75,
                  py: 1.5,
                  borderRadius: '10px',
                  bgcolor: 'rgba(255,255,255,.14)',
                }}
              >
                <Typography
                  sx={{
                    fontSize: 10,
                    letterSpacing: '.14em',
                    textTransform: 'uppercase',
                    fontWeight: 600,
                    color: 'rgba(255,255,255,.75)',
                  }}
                >
                  ISBN
                </Typography>
                <Stack direction="row" spacing={1} sx={{ mt: 0.5, alignItems: 'center' }}>
                  <Typography
                    sx={{ fontSize: 15, fontWeight: 600, color: '#fff', fontVariantNumeric: 'tabular-nums' }}
                  >
                    {data.isbn}
                  </Typography>
                  {/* The prototype offers a copy control beside the ISBN, and it earns its place:
                      this is the one value on the screen somebody retypes into another system. */}
                  <IconButton
                    aria-label="Copy ISBN"
                    onClick={() => void navigator.clipboard?.writeText(data.isbn)}
                    sx={{
                      width: 26,
                      height: 26,
                      color: '#fff',
                      border: '1px solid rgba(255,255,255,.45)',
                    }}
                  >
                    <MaterialSymbol name="content_copy" size={14} />
                  </IconButton>
                </Stack>
              </Box>
            </Box>

            <Stack sx={{ minWidth: 0, maxHeight: 'calc(100vh - 64px)' }}>
              <Box sx={{ flex: 1, minHeight: 0, overflowY: 'auto', p: 3.5 }}>
                <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
                  <Chip
                    size="small"
                    label={GENRE_LABEL[data.genre]}
                    sx={{ bgcolor: 'rgba(14,90,110,.10)', color: 'primary.main' }}
                  />
                  <Typography variant="caption" color="text.secondary">
                    {data.tier} catalog
                  </Typography>
                  <Stack
                    direction="row"
                    spacing={0.5}
                    sx={{ ml: 'auto', alignItems: 'center' }}
                  >
                    <MaterialSymbol name="star" size={16} sx={{ color: '#E0A63C' }} />
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {/* No reviews reports no rating, never a zero — BR-CAT-030. */}
                      {data.averageRating === null
                        ? 'Not rated yet'
                        : `${data.averageRating.toFixed(1)} · ${data.reviewCount} ${data.reviewCount === 1 ? 'review' : 'reviews'}`}
                    </Typography>
                  </Stack>
                </Stack>

                {data.badge ? (
                  <Alert severity="warning" icon={<MaterialSymbol name="info" size={20} />} sx={{ mt: 2 }}>
                    {bookBadgeLabel(data.badge, membership)}
                  </Alert>
                ) : null}

                {/* The prototype's tinted card, `location_on` and all. It answers the first
                    question somebody opening a book actually has — where is it — and answering it
                    in a panel of its own is why it does not read as one more list. */}
                <Box
                  sx={{
                    mt: 2.75,
                    p: 2.25,
                    borderRadius: '12px',
                    border: 1,
                    borderColor: 'divider',
                    bgcolor: 'rgba(14,90,110,.05)',
                  }}
                >
                  <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
                    <MaterialSymbol
                      name="location_on"
                      size={22}
                      sx={{ color: 'primary.main', flexShrink: 0, mt: 0.25 }}
                    />
                    <Stack spacing={1.25} sx={{ minWidth: 0, flex: 1 }}>
                      <Typography variant="overline" color="text.secondary">
                        Where to find it
                      </Typography>

                      {data.copies.length === 0 ? (
                        <Typography variant="body2" color="text.secondary">
                          No branch holds a copy at the moment.
                        </Typography>
                      ) : (
                        data.copies.map((copy) => (
                          <BranchRow key={copy.libraryId} copy={copy} membership={membership} />
                        ))
                      )}
                    </Stack>
                  </Stack>
                </Box>

                <Section title="Availability">
                  <Box
                    sx={{
                      display: 'grid',
                      gap: 1.25,
                      // The prototype's four across; two on a narrow panel rather than four unreadable slivers.
                      gridTemplateColumns: { xs: 'repeat(2, 1fr)', sm: 'repeat(4, 1fr)' },
                    }}
                  >
                    <Tile label="On shelf" value={String(onShelf)} />
                    <Tile label="On loan" value={String(Math.max(total - onShelf, 0))} />
                    <Tile label="Total copies" value={String(total)} />
                    <Tile label="Branches" value={String(data.copies.length)} />
                  </Box>
                </Section>

                <Section title="Catalog record">
                  <Box
                    sx={{
                      display: 'grid',
                      gap: '12px 20px',
                      gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
                    }}
                  >
                    {data.publisher ? <Record label="Publisher" value={data.publisher} /> : null}
                    <Record label="Genre" value={GENRE_LABEL[data.genre]} />
                    <Record label="Plan required" value={data.tier} />
                    <Record label="Retail price" value={money(data.retailPriceCents)} />
                  </Box>
                </Section>
              </Box>

              <Stack
                direction="row"
                spacing={1.25}
                sx={{ flexShrink: 0, px: 3.5, py: 2.25, borderTop: 1, borderColor: 'divider' }}
              >
                <Button
                  variant="contained"
                  disabled={!data.canReserve}
                  onClick={() => onReserve(data.id)}
                  sx={{ flex: 1, height: 42, borderRadius: '21px' }}
                >
                  {data.canReserve ? 'Reserve 14 days' : 'Unavailable'}
                </Button>
                {/* Buying is always offered: BR-STR-012 makes reach decide the discount, never the
                    right to buy. A book nobody can borrow can still be owned. */}
                <Button
                  variant="outlined"
                  onClick={() => onBuy(data.id)}
                  sx={{ flex: 1, height: 42, borderRadius: '21px' }}
                >
                  Buy {money(data.retailPriceCents)}
                </Button>
                <IconButton
                  onClick={onClose}
                  aria-label="Close"
                  sx={{ width: 42, height: 42, border: 1, borderColor: 'divider', flexShrink: 0 }}
                >
                  <MaterialSymbol name="close" size={20} />
                </IconButton>
              </Stack>
            </Stack>
          </Box>

        </>
      )}
    </Dialog>
  );
};

const Section = ({ title, children }: { title: string; children: React.ReactNode }) => (
  <Stack spacing={1.5} sx={{ mt: 2.75 }}>
    <Typography variant="overline" color="text.secondary">
      {title}
    </Typography>
    {children}
  </Stack>
);

const Tile = ({ label, value }: { label: string; value: string }) => (
  <Box sx={{ px: 1.75, py: 1.5, border: 1, borderColor: 'divider', borderRadius: '10px' }}>
    <Typography
      sx={{
        fontSize: 10,
        letterSpacing: '.1em',
        textTransform: 'uppercase',
        fontWeight: 600,
        color: 'text.secondary',
      }}
    >
      {label}
    </Typography>
    <Typography sx={{ mt: 0.625, fontSize: 16, fontWeight: 600 }}>{value}</Typography>
  </Box>
);

const Record = ({ label, value }: { label: string; value: string }) => (
  <Box sx={{ pb: 1.25, borderBottom: 1, borderColor: 'divider', minWidth: 0 }}>
    <Typography
      sx={{
        fontSize: 11,
        letterSpacing: '.1em',
        textTransform: 'uppercase',
        fontWeight: 600,
        color: 'text.secondary',
      }}
    >
      {label}
    </Typography>
    <Typography variant="body2" sx={{ mt: 0.375, fontWeight: 600, overflowWrap: 'anywhere' }}>
      {value}
    </Typography>
  </Box>
);

const BranchRow = ({
  copy,
  membership,
}: {
  copy: CopyAvailability;
  membership: Membership | undefined;
}) => (
  <Stack direction="row" spacing={2} sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
    <Stack spacing={0.25} sx={{ minWidth: 0 }}>
      <Typography variant="body2" noWrap>
        {copy.cityName} — {copy.libraryName}
      </Typography>
      <Typography variant="caption" color="text.secondary">
        {copy.availableCount} of {copy.totalCount} on shelf
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
);

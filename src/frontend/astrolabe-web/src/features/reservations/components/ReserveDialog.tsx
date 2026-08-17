import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  Stack,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { BookCover } from '../../catalog/components/BookCover';
import { formatDate, money } from '../../membership/planCopy';
import {
  confirmReservation,
  quoteReservation,
  type DeliveryMethod,
  type ReservableCopy,
} from '../api/reservationsApi';
import { DELIVERY_LABEL, DELIVERY_NOTE } from '../reservationCopy';
import { useMemberDefaults } from '../../settings/memberDefaults';

/**
 * The reservation modal, opened from the catalogue.
 *
 * Every figure comes from the quote the API returned — the fee, the due date and each branch's
 * verdict. Nothing is recomputed here: the same rule refuses the loan on the server, and a second
 * implementation in the browser would eventually contradict it in front of the member.
 */
export interface ReserveDialogProps {
  bookId: string | null;
  onClose: () => void;
  onReserved: () => void;
}

export const ReserveDialog = ({ bookId, onClose, onReserved }: ReserveDialogProps) => {
  const queryClient = useQueryClient();
  const preferredDelivery = useMemberDefaults((state) => state.delivery);
  const [delivery, setDelivery] = useState<DeliveryMethod>(preferredDelivery);
  const [libraryId, setLibraryId] = useState<string | null>(null);

  const quote = useQuery({
    queryKey: ['reservations', 'quote', bookId, delivery],
    queryFn: () => quoteReservation(bookId!, delivery),
    enabled: bookId !== null,
  });

  /**
   * One key per attempt the member makes, not per request. A retry after a dropped connection
   * reuses it and is a no-op; reopening the modal earns a new one, so a deliberate second
   * reservation is still possible.
   */
  const idempotencyKey = useMemo(
    () => (bookId ? `${bookId}:${crypto.randomUUID()}` : ''),
    [bookId],
  );

  // The first branch the member can actually borrow from is pre-selected, so the common case is one
  // click. Choosing a closed one for them would only produce a refusal.
  useEffect(() => {
    const first = quote.data?.copies.find((copy) => copy.canReserve);
    setLibraryId(first?.libraryId ?? null);
  }, [quote.data]);

  const confirm = useMutation({
    meta: { success: 'Reserved. It is in your reservations now.', silent: true },
    mutationFn: () => confirmReservation(bookId!, libraryId!, delivery, idempotencyKey),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['reservations'] });
      await queryClient.invalidateQueries({ queryKey: ['catalog'] });
      onReserved();
    },
  });

  const close = () => {
    confirm.reset();
    setDelivery(preferredDelivery);
    onClose();
  };

  const selected = quote.data?.copies.find((copy) => copy.libraryId === libraryId) ?? null;

  return (
    <Dialog open={bookId !== null} onClose={confirm.isPending ? undefined : close} maxWidth="sm" fullWidth>
      {quote.isLoading || !quote.data ? (
        <DialogContent>
          {quote.isError ? (
            <ErrorState description="We could not price that reservation." onRetry={() => void quote.refetch()} />
          ) : (
            <LoadingState label="Checking availability…" />
          )}
        </DialogContent>
      ) : (
        <>
          {/* The prototype's header: a 52x74 cover, the kicker, the title in serif, then
              `{author} · {genre} · {tier} catalog`, and a round close button. */}
          <DialogTitle sx={{ display: 'flex', alignItems: 'flex-start', gap: 2, pb: 2 }}>
            <Box sx={{ width: 52, flexShrink: 0 }}>
              <BookCover
                bookId={quote.data.bookId}
                title={quote.data.title}
                coverUrl={quote.data.coverUrl}
                height={74}
              />
            </Box>
            <Stack spacing={0.5} sx={{ flex: 1, minWidth: 0 }}>
              <Typography variant="overline" color="text.secondary">
                Confirm reservation
              </Typography>
              <Typography variant="h5">{quote.data.title}</Typography>
              <Typography variant="caption" color="text.secondary">
                {quote.data.author} · {quote.data.genre} · {quote.data.tier} catalog
              </Typography>
            </Stack>
            <IconButton
              aria-label="Close"
              onClick={close}
              disabled={confirm.isPending}
              sx={{ mt: -0.5 }}
            >
              <MaterialSymbol name="close" size={18} />
            </IconButton>
          </DialogTitle>

          <DialogContent>
            <Stack spacing={2.5}>
              <Stack spacing={1}>
                <Typography variant="subtitle2">Choose a copy</Typography>
                {quote.data.copies.map((copy) => (
                  <CopyOption
                    key={copy.libraryId}
                    copy={copy}
                    selected={copy.libraryId === libraryId}
                    onSelect={() => setLibraryId(copy.libraryId)}
                  />
                ))}
              </Stack>

              <Divider />

              <Stack spacing={1}>
                <Stack direction="row" spacing={1.25} sx={{ alignItems: 'baseline', flexWrap: 'wrap' }}>
                  <Typography variant="overline" color="text.secondary">
                    How do you want it?
                  </Typography>
                  {/* Names the saved default rather than silently applying it. A member who set one
                      in Settings should see it honoured here, and one who did not should learn the
                      setting exists at the moment it would have helped. */}
                  <Typography variant="caption" color="text.secondary">
                    Your default is {DELIVERY_LABEL[preferredDelivery].toLowerCase()}.
                  </Typography>
                </Stack>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                  {(['Collection', 'HomeDelivery'] as const).map((method) => (
                    <Card
                      key={method}
                      variant="outlined"
                      sx={{
                        flex: 1,
                        borderColor: delivery === method ? 'primary.main' : 'divider',
                        borderWidth: delivery === method ? 2 : 1,
                        bgcolor: delivery === method ? 'action.selected' : 'transparent',
                      }}
                    >
                      <CardActionArea sx={{ p: 1.5 }} onClick={() => setDelivery(method)}>
                        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                          <MaterialSymbol
                            name={delivery === method ? 'radio_button_checked' : 'radio_button_unchecked'}
                            size={18}
                            sx={{ color: delivery === method ? 'primary.main' : 'text.secondary' }}
                          />
                          <Stack spacing={0.25}>
                            <Typography variant="body2">{DELIVERY_LABEL[method]}</Typography>
                            <Typography variant="caption" color="text.secondary">
                              {DELIVERY_NOTE[method]}
                            </Typography>
                          </Stack>
                        </Stack>
                      </CardActionArea>
                    </Card>
                  ))}
                </Stack>
              </Stack>

              <Divider />

              <Stack spacing={0.75} sx={{ p: 2, borderRadius: '10px', border: 1, borderColor: 'divider' }}>
                <Row label="Loan" value={money(0)} />
                <Row label="Delivery" value={money(quote.data.deliveryFeeCents)} />
                <Row label="Due back" value={formatDate(quote.data.dueOn)} />
                <Stack
                  direction="row"
                  sx={{ justifyContent: 'space-between', alignItems: 'baseline', pt: 0.5 }}
                >
                  <Typography variant="subtitle2">Due today</Typography>
                  <Typography variant="h6">{money(quote.data.totalCents)}</Typography>
                </Stack>
              </Stack>

              {confirm.isError ? (
                <Alert severity="error">
                  {(confirm.error as { response?: { data?: { title?: string } } })?.response?.data
                    ?.title ?? 'We could not complete that reservation.'}
                </Alert>
              ) : null}
            </Stack>
          </DialogContent>

          <DialogActions>
            <Button onClick={close} color="inherit" disabled={confirm.isPending}>
              Cancel
            </Button>
            <Button
              variant="contained"
              // Without a reachable copy there is nothing to confirm, and offering the button would
              // only produce the refusal the member can already see beside each branch.
              disabled={!selected?.canReserve}
              loading={confirm.isPending}
              onClick={() => confirm.mutate()}
            >
              Reserve for 14 days
            </Button>
          </DialogActions>
        </>
      )}
    </Dialog>
  );
};

const CopyOption = ({
  copy,
  selected,
  onSelect,
}: {
  copy: ReservableCopy;
  selected: boolean;
  onSelect: () => void;
}) => (
  <Card
    variant="outlined"
    sx={{
      borderColor: selected && copy.canReserve ? 'primary.main' : 'divider',
      // A copy the member cannot take is shown, not hidden: seeing where the book is, and why it is
      // out of reach, is the point of listing every branch.
      opacity: copy.canReserve ? 1 : 0.6,
    }}
  >
    <CardActionArea disabled={!copy.canReserve} onClick={onSelect} sx={{ p: 1.25 }}>
      <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
        <MaterialSymbol
          name={
            !copy.canReserve ? 'block' : selected ? 'radio_button_checked' : 'radio_button_unchecked'
          }
          size={18}
          sx={{ color: selected && copy.canReserve ? 'primary.main' : 'text.secondary' }}
        />
        <Stack spacing={0.25} sx={{ flex: 1, minWidth: 0 }}>
          <Typography variant="body2">
            {copy.cityName} — {copy.libraryName}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {copy.availableCount === 0
              ? 'No copies on shelf'
              : `${copy.availableCount} ${copy.availableCount === 1 ? 'copy' : 'copies'} on shelf`}
          </Typography>
        </Stack>
        {copy.canReserve ? null : (
          <Chip size="small" variant="outlined" label={copy.reason ?? 'Unavailable'} />
        )}
      </Stack>
    </CardActionArea>
  </Card>
);

const Row = ({ label, value }: { label: string; value: string }) => (
  <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
    <Typography variant="body2" color="text.secondary">
      {label}
    </Typography>
    <Typography variant="body2">{value}</Typography>
  </Stack>
);

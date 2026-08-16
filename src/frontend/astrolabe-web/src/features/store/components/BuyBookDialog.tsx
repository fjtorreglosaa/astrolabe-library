import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Stack,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { getMyPaymentMethods } from '../../billing/api/billingApi';
import { BookCover } from '../../catalog/components/BookCover';
import { money } from '../../membership/planCopy';
import { placeOrder, quoteOrder, type OrderFulfilment } from '../api/storeApi';
import { FULFILMENT_LABEL, FULFILMENT_NOTE, PURCHASE_IS_A_NEW_COPY } from '../storeCopy';

/**
 * The purchase modal, opened from a book.
 *
 * Every figure comes from the quote the API returned, priced by the same policy the purchase uses.
 * Recomputing a discount here would put money arithmetic in two languages, and the day they disagree
 * the member is charged something other than what they agreed to.
 */
export interface BuyBookDialogProps {
  bookId: string | null;
  title: string;
  coverUrl: string | null;
  onClose: () => void;
}

export const BuyBookDialog = ({ bookId, title, coverUrl, onClose }: BuyBookDialogProps) => {
  const queryClient = useQueryClient();
  const [fulfilment, setFulfilment] = useState<OrderFulfilment>('Collection');
  const [placed, setPlaced] = useState<{ total: number; points: number } | null>(null);

  const quote = useQuery({
    queryKey: ['store', 'quote', bookId, fulfilment],
    queryFn: () => quoteOrder([bookId!], fulfilment),
    enabled: bookId !== null,
  });

  const cards = useQuery({
    queryKey: ['billing', 'payment-methods'],
    queryFn: getMyPaymentMethods,
    enabled: bookId !== null,
  });

  const card = cards.data?.find((method) => method.isPrimary) ?? cards.data?.[0];

  // One key per attempt the member makes, not per request. A retry after a dropped connection
  // reuses it; reopening the modal earns a new one, so buying a second copy stays possible.
  const idempotencyKey = useMemo(
    () => (bookId ? `${bookId}:${crypto.randomUUID()}` : ''),
    [bookId],
  );

  const buy = useMutation({
    mutationFn: () =>
      placeOrder({ bookId: bookId!, fulfilment, paymentMethodId: card!.id, idempotencyKey }),
    onSuccess: async (order) => {
      setPlaced({ total: order.totalCents, points: order.pointsEarned });
      await queryClient.invalidateQueries({ queryKey: ['store'] });
      await queryClient.invalidateQueries({ queryKey: ['billing'] });
    },
  });

  const close = () => {
    buy.reset();
    setPlaced(null);
    setFulfilment('Collection');
    onClose();
  };

  return (
    <Dialog open={bookId !== null} onClose={buy.isPending ? undefined : close} maxWidth="sm" fullWidth>
      {placed ? (
        <>
          <DialogTitle>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <MaterialSymbol name="check_circle" size={24} sx={{ color: 'success.main' }} />
              <Typography variant="h6">Purchase complete</Typography>
            </Stack>
          </DialogTitle>
          <DialogContent>
            <Stack spacing={1.5}>
              <Typography variant="body2" color="text.secondary">
                We charged {money(placed.total)} to {card?.displayName}.{' '}
                {fulfilment === 'Shipping'
                  ? 'It ships in 3–5 days.'
                  : 'Collect it at the library in 2 hours.'}
              </Typography>
              {placed.points > 0 ? (
                <Alert severity="success" icon={<MaterialSymbol name="stars" size={20} />}>
                  You earned {placed.points} reward points on this purchase.
                </Alert>
              ) : null}
              <Typography variant="caption" color="text.secondary">
                {PURCHASE_IS_A_NEW_COPY}
              </Typography>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button variant="contained" onClick={close}>
              Done
            </Button>
          </DialogActions>
        </>
      ) : quote.isLoading || !quote.data ? (
        <DialogContent>
          {quote.isError ? (
            <ErrorState description="We could not price that book." onRetry={() => void quote.refetch()} />
          ) : (
            <LoadingState label="Pricing…" />
          )}
        </DialogContent>
      ) : (
        <>
          <DialogTitle sx={{ pb: 1 }}>
            <Stack direction="row" spacing={2}>
              <Box sx={{ width: 56, flexShrink: 0 }}>
                <BookCover bookId={bookId!} title={title} coverUrl={coverUrl} height={78} />
              </Box>
              <Stack spacing={0.5} sx={{ minWidth: 0 }}>
                <Typography variant="h6">Buy this book</Typography>
                <Typography variant="body2" color="text.secondary" noWrap>
                  {title}
                </Typography>
              </Stack>
            </Stack>
          </DialogTitle>

          <DialogContent>
            <Stack spacing={2.5}>
              <Stack spacing={1}>
                <Typography variant="subtitle2">How would you like it?</Typography>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                  {(['Collection', 'Shipping'] as const).map((option) => (
                    <Card
                      key={option}
                      variant="outlined"
                      sx={{
                        flex: 1,
                        borderColor: fulfilment === option ? 'primary.main' : 'divider',
                        borderWidth: fulfilment === option ? 2 : 1,
                      }}
                    >
                      <CardActionArea sx={{ p: 1.25 }} onClick={() => setFulfilment(option)}>
                        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                          <MaterialSymbol
                            name={
                              fulfilment === option ? 'radio_button_checked' : 'radio_button_unchecked'
                            }
                            size={18}
                            sx={{ color: fulfilment === option ? 'primary.main' : 'text.secondary' }}
                          />
                          <Stack spacing={0.25}>
                            <Typography variant="body2">{FULFILMENT_LABEL[option]}</Typography>
                            <Typography variant="caption" color="text.secondary">
                              {FULFILMENT_NOTE[option]}
                            </Typography>
                          </Stack>
                        </Stack>
                      </CardActionArea>
                    </Card>
                  ))}
                </Stack>
              </Stack>

              <Divider />

              <Stack spacing={0.75}>
                <Row label="Price" value={money(quote.data.subtotalCents)} />
                {quote.data.discountTotalCents > 0 ? (
                  <Row
                    label={`${quote.data.lines[0]?.discountPercent}% plan discount`}
                    value={`−${money(quote.data.discountTotalCents)}`}
                  />
                ) : null}
                <Row
                  label="Delivery"
                  value={quote.data.shippingFeeCents === 0 ? 'Free' : money(quote.data.shippingFeeCents)}
                />
                <Stack
                  direction="row"
                  sx={{ justifyContent: 'space-between', alignItems: 'baseline', pt: 0.5 }}
                >
                  <Typography variant="subtitle2">Total</Typography>
                  <Typography variant="h6">{money(quote.data.totalCents)}</Typography>
                </Stack>

                {/* A Plus member shown 0% is entitled to know that is the rule, not a fault. */}
                <Typography variant="caption" color="text.secondary">
                  {quote.data.discountNote}
                </Typography>

                {quote.data.pointsWouldEarn > 0 ? (
                  <Typography variant="caption" color="success.main">
                    Earns {quote.data.pointsWouldEarn} reward points.
                  </Typography>
                ) : null}
              </Stack>

              {card ? (
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                  <MaterialSymbol name="credit_card" size={20} sx={{ color: 'text.secondary' }} />
                  <Typography variant="body2" color="text.secondary">
                    Paying with {card.displayName}
                  </Typography>
                </Stack>
              ) : (
                <Alert severity="warning">
                  Add a payment method in Fines &amp; payments before buying.
                </Alert>
              )}

              {buy.isError ? (
                <Alert severity="error">
                  {(buy.error as { response?: { data?: { title?: string } } })?.response?.data
                    ?.title ?? 'We could not complete that purchase.'}
                </Alert>
              ) : null}
            </Stack>
          </DialogContent>

          <DialogActions>
            <Button onClick={close} color="inherit" disabled={buy.isPending}>
              Cancel
            </Button>
            <Button
              variant="contained"
              disabled={!card}
              loading={buy.isPending}
              onClick={() => buy.mutate()}
            >
              Buy for {money(quote.data.totalCents)}
            </Button>
          </DialogActions>
        </>
      )}
    </Dialog>
  );
};

const Row = ({ label, value }: { label: string; value: string }) => (
  <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
    <Typography variant="body2" color="text.secondary">
      {label}
    </Typography>
    <Typography variant="body2">{value}</Typography>
  </Stack>
);

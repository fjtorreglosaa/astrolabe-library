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
  IconButton,
  Divider,
  Slider,
  Stack,
  Switch,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { getMyPaymentMethods } from '../../billing/api/billingApi';
import { BookCover } from '../../catalog/components/BookCover';
import { useMemberDefaults } from '../../settings/memberDefaults';
import { money } from '../../membership/planCopy';
import { placeOrder, quoteOrder, type OrderFulfilment } from '../api/storeApi';
import {
  FULFILMENT_LABEL,
  FULFILMENT_NOTE,
  PURCHASE_IS_A_NEW_COPY,
  REDEMPTION_RULE_NOTE,
  pointsAsMoney,
} from '../storeCopy';

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
  // The member's saved default, as the prototype reads `prefs.purchase`. They can still switch it
  // here — the section says so, which is the whole point of calling it a default.
  const preferredFulfilment = useMemberDefaults((state) => state.purchase);
  const [fulfilment, setFulfilment] = useState<OrderFulfilment>(preferredFulfilment);
  const [usePoints, setUsePoints] = useState(false);
  const [points, setPoints] = useState<number | null>(null);
  const [placed, setPlaced] = useState<
    { total: number; charged: number; points: number; redeemed: number } | null
  >(null);

  // Priced with no redemption first, because the cap depends on what the books come to and the
  // member has not chosen an amount yet. This is also what tells us whether to offer the control.
  const base = useQuery({
    queryKey: ['store', 'quote', bookId, fulfilment, 0],
    queryFn: () => quoteOrder([bookId!], fulfilment, 0),
    enabled: bookId !== null,
  });

  const maxPoints = base.data?.maxRedeemablePointCents ?? 0;

  // Defaults to spending everything the order will take. A member who opens the switch wants to use
  // their points; making them drag from zero to find that out is a worse first move.
  const chosen = usePoints ? Math.min(points ?? maxPoints, maxPoints) : 0;

  const quote = useQuery({
    queryKey: ['store', 'quote', bookId, fulfilment, chosen],
    queryFn: () => quoteOrder([bookId!], fulfilment, chosen),
    enabled: bookId !== null && chosen > 0,
  });

  // The zero-redemption quote is a valid quote in its own right, so it is reused rather than
  // fetched twice.
  const priced = chosen > 0 ? quote.data : base.data;

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
    meta: { success: 'Purchase complete. It is in your orders.', silent: true },
    mutationFn: () =>
      placeOrder({
        bookId: bookId!,
        fulfilment,
        paymentMethodId: card!.id,
        idempotencyKey,
        // Sent from the priced quote, never from the slider: the two can differ for a moment while
        // a request is in flight, and the member must be charged what they were last shown.
        pointsToRedeem: priced?.pointsRedeemed ?? 0,
      }),
    onSuccess: async (order) => {
      setPlaced({
        total: order.totalCents,
        charged: order.amountChargedCents,
        points: order.pointsEarned,
        redeemed: order.pointsRedeemed,
      });
      await queryClient.invalidateQueries({ queryKey: ['store'] });
      await queryClient.invalidateQueries({ queryKey: ['billing'] });
    },
  });

  const close = () => {
    buy.reset();
    setPlaced(null);
    // Back to the member's own default, not to a literal. Resetting to Collection would quietly
    // override somebody who set Shipping in Settings, every time they closed the dialog.
    setFulfilment(preferredFulfilment);
    setUsePoints(false);
    setPoints(null);
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
                We charged {money(placed.charged)} to {card?.displayName}.{' '}
                {fulfilment === 'Shipping'
                  ? 'It ships in 3–5 days.'
                  : 'Collect it at the library in 2 hours.'}
              </Typography>
              {placed.redeemed > 0 ? (
                <Typography variant="body2" color="text.secondary">
                  {placed.redeemed} reward points covered the remaining{' '}
                  {pointsAsMoney(placed.redeemed)} of {money(placed.total)}.
                </Typography>
              ) : null}
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
      ) : !priced ? (
        <DialogContent>
          {base.isError ? (
            <ErrorState description="We could not price that book." onRetry={() => void base.refetch()} />
          ) : (
            <LoadingState label="Pricing…" />
          )}
        </DialogContent>
      ) : (
        <>
          <DialogTitle sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.75, pb: 2 }}>
            <Box sx={{ width: 38, flexShrink: 0 }}>
              <BookCover bookId={bookId!} title={title} coverUrl={coverUrl} height={54} />
            </Box>
            <Stack spacing={0.5} sx={{ flex: 1, minWidth: 0 }}>
              <Typography variant="overline" color="text.secondary">
                Buy this book
              </Typography>
              <Typography variant="h5">{title}</Typography>
            </Stack>
            <IconButton
              aria-label="Close"
              onClick={close}
              disabled={buy.isPending}
              sx={{ mt: -0.5 }}
            >
              <MaterialSymbol name="close" size={19} />
            </IconButton>
          </DialogTitle>

          <DialogContent>
            <Stack spacing={2.5}>
              <Stack spacing={1}>
                <Stack direction="row" spacing={1.25} sx={{ alignItems: 'baseline', flexWrap: 'wrap' }}>
                  <Typography variant="overline" color="text.secondary">
                    How do you want it?
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Your default is {FULFILMENT_LABEL[preferredFulfilment].toLowerCase()}.
                  </Typography>
                </Stack>
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

              <Stack
                spacing={1}
                sx={{ p: 2, borderRadius: '10px', border: 1, borderColor: 'divider' }}
              >
                <Row label="Book" value={money(priced.subtotalCents)} />
                {priced.discountTotalCents > 0 ? (
                  <Row
                    label={`${priced.lines[0]?.discountPercent}% plan discount`}
                    value={`−${money(priced.discountTotalCents)}`}
                  />
                ) : null}
                <Row
                  label="Shipping"
                  value={priced.shippingFeeCents === 0 ? 'Free' : money(priced.shippingFeeCents)}
                />
                <Box sx={{ height: '1px', bgcolor: 'divider' }} />
                <Stack
                  direction="row"
                  sx={{ justifyContent: 'space-between', alignItems: 'baseline' }}
                >
                  <Typography variant="body2" color="text.secondary">
                    Total
                  </Typography>
                  {/* The prototype sets the figure in serif at 22px — it is the number the member
                      is agreeing to, and it should not read like another row. */}
                  <Typography variant="h4">{money(priced.totalCents)}</Typography>
                </Stack>

                {/* A Plus member shown 0% is entitled to know that is the rule, not a fault. */}
                <Typography variant="caption" color="text.secondary">
                  {priced.discountNote}
                </Typography>

                {priced.pointsWouldEarn > 0 ? (
                  <Typography variant="caption" color="success.main">
                    Earns {priced.pointsWouldEarn} reward points.
                  </Typography>
                ) : null}
              </Stack>

              {/* Reward points, BR-STR-007. The control appears only when this order can actually
                  take some — a slider that cannot move is worse than no slider, and the note says
                  which of the two rules is biting. */}
              {priced.pointsBalance > 0 ? (
                <>
                  <Divider />
                  <Stack spacing={1}>
                    <Stack
                      direction="row"
                      spacing={1}
                      sx={{ alignItems: 'center', justifyContent: 'space-between' }}
                    >
                      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                        <MaterialSymbol
                          name="stars"
                          size={20}
                          sx={{ color: maxPoints > 0 ? 'primary.main' : 'text.disabled' }}
                        />
                        <Stack spacing={0.25}>
                          <Typography variant="subtitle2">Use reward points</Typography>
                          <Typography variant="caption" color="text.secondary">
                            {priced.redemptionNote}
                          </Typography>
                        </Stack>
                      </Stack>
                      <Switch
                        checked={usePoints && maxPoints > 0}
                        disabled={maxPoints === 0}
                        onChange={(event) => setUsePoints(event.target.checked)}
                        slotProps={{ input: { 'aria-label': 'Use reward points' } }}
                      />
                    </Stack>

                    {usePoints && maxPoints > 0 ? (
                      <Stack spacing={0.5} sx={{ px: 0.5 }}>
                        <Slider
                          value={chosen}
                          min={100}
                          max={maxPoints}
                          step={1}
                          marks={[
                            { value: 100, label: '100' },
                            { value: maxPoints, label: `${maxPoints}` },
                          ]}
                          onChange={(_event, value) => setPoints(value as number)}
                          aria-label="Reward points to apply"
                        />
                        <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                          <Typography variant="caption" color="text.secondary">
                            {REDEMPTION_RULE_NOTE}
                          </Typography>
                          <Typography variant="caption" color="primary.main">
                            −{pointsAsMoney(priced.pointsRedeemed)}
                          </Typography>
                        </Stack>
                      </Stack>
                    ) : null}
                  </Stack>
                </>
              ) : null}

              {card ? (
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                  <MaterialSymbol name="credit_card" size={20} sx={{ color: 'text.secondary' }} />
                  <Typography variant="body2" color="text.secondary">
                    Paying {money(priced.amountChargedCents)} with {card.displayName}
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

          <DialogActions sx={{ px: 3, py: 2 }}>
            <Button
              variant="contained"
              disabled={!card}
              loading={buy.isPending}
              onClick={() => buy.mutate()}
              sx={{ flex: 1, height: 46 }}
            >
              Confirm purchase
            </Button>
            <Button onClick={close} color="inherit" disabled={buy.isPending} sx={{ height: 46 }}>
              Cancel
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

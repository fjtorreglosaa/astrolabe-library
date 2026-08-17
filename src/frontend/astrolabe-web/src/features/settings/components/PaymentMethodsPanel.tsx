import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState } from '../../../shared/components/StateViews';
import { ListRowsSkeleton } from '../../../shared/components/ListRowsSkeleton';
import {
  addPaymentMethod,
  getMyPaymentMethods,
  removePaymentMethod,
  type CardBrand,
  type PaymentMethod,
} from '../../billing/api/billingApi';

const BRANDS: CardBrand[] = ['Visa', 'Mastercard', 'Amex'];

/**
 * Cards on file.
 *
 * <p>
 * <b>There is no field for a card number here, and there is none on the API either.</b> What this
 * form collects is what a tokenising provider hands back afterwards — brand, last four, expiry,
 * cardholder — and the server refuses anything but four digits rather than truncating a full number
 * into storage. The note says so, because a form asking for card details without explaining that is
 * a form people are right to distrust.
 * </p>
 */
export const PaymentMethodsPanel = () => {
  const queryClient = useQueryClient();
  const [adding, setAdding] = useState(false);
  const [removing, setRemoving] = useState<PaymentMethod | null>(null);
  const [brand, setBrand] = useState<CardBrand>('Visa');
  const [last4, setLast4] = useState('');
  const [expiry, setExpiry] = useState('');
  const [holder, setHolder] = useState('');
  const [primary, setPrimary] = useState(false);

  const cards = useQuery({
    queryKey: ['billing', 'payment-methods'],
    queryFn: getMyPaymentMethods,
  });

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['billing'] });

  const add = useMutation({
    meta: { silent: true },
    mutationFn: () =>
      addPaymentMethod({
        brand,
        last4: last4.trim(),
        expiryMonthYear: expiry.trim(),
        cardholderName: holder.trim(),
        makePrimary: primary,
      }),
    onSuccess: async () => {
      setAdding(false);
      setLast4('');
      setExpiry('');
      setHolder('');
      setPrimary(false);
      await refresh();
    },
  });

  const remove = useMutation({
    mutationFn: () => removePaymentMethod(removing!.id),
    onSuccess: async () => {
      setRemoving(null);
      await refresh();
    },
  });

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Stack spacing={0.25}>
          <Typography variant="h6">Payment methods</Typography>
          <Typography variant="body2" color="text.secondary">
            Used for fines, purchases and delivery charges.
          </Typography>
        </Stack>
        <Button
          startIcon={<MaterialSymbol name="add" size={20} />}
          onClick={() => setAdding(true)}
        >
          Add method
        </Button>
      </Stack>

      {cards.isLoading ? (
        <ListRowsSkeleton rows={2} label="Loading your cards" />
      ) : cards.isError || !cards.data ? (
        <ErrorState description="We could not load your cards." onRetry={() => void cards.refetch()} />
      ) : cards.data.length === 0 ? (
        <EmptyState
          title="No card on file"
          description="Add one and paying a fine or buying a book takes a single click."
        />
      ) : (
        // The prototype's grid: `repeat(auto-fill, minmax(280px, 1fr))`, gap 14. Cards side by side
        // rather than stacked full-width — a card is a small, fixed amount of information, and a row
        // the width of the page spends most of itself on emptiness between the brand and the button.
        <Box
          sx={{
            display: 'grid',
            gap: 1.75,
            gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
          }}
        >
          {cards.data.map((card) => (
            <Paper
              key={card.id}
              variant="outlined"
              sx={{ p: 2.25, borderRadius: '12px', display: 'flex', flexDirection: 'column', gap: 1.5 }}
            >
              <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
                {/* The prototype's 38x26 chip — a card shape, not a generic icon. */}
                <Box
                  aria-hidden
                  sx={{
                    width: 38,
                    height: 26,
                    flexShrink: 0,
                    borderRadius: '5px',
                    bgcolor: '#0B2E3B',
                    color: '#fff',
                    display: 'grid',
                    placeItems: 'center',
                  }}
                >
                  <MaterialSymbol name="credit_card" size={16} />
                </Box>

                <Stack spacing={0.25} sx={{ flex: 1, minWidth: 0 }}>
                  <Typography variant="body2" sx={{ fontWeight: 600 }} noWrap>
                    {card.brand} •••• {card.last4}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" noWrap>
                    Expires {card.expiryMonthYear} · {card.cardholderName}
                  </Typography>
                </Stack>

                {card.isPrimary ? (
                  <Chip
                    size="small"
                    label="Default"
                    sx={{
                      flexShrink: 0,
                      bgcolor: 'rgba(16,168,140,.14)',
                      color: '#0C7F70',
                    }}
                  />
                ) : null}
              </Stack>

              <Stack direction="row" spacing={1}>
                {/* The prototype also offers "Make default" here. There is no endpoint for it —
                    a card is made primary when it is added, and nothing promotes an existing one —
                    so the control is left out rather than shown and refused. Raised as GLOBAL-028. */}
                <Button
                  size="small"
                  color="error"
                  variant="outlined"
                  onClick={() => setRemoving(card)}
                  sx={{ height: 34, borderRadius: '17px' }}
                >
                  Remove
                </Button>
              </Stack>
            </Paper>
          ))}
        </Box>
      )}

      <Alert severity="info" icon={<MaterialSymbol name="lock" size={20} />}>
        Card details are tokenised by the payment processor. Astrolabe stores only the brand and the
        last four digits — there is no field anywhere in this system that could hold a full number.
      </Alert>

      <Dialog open={adding} onClose={() => setAdding(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Add a payment method</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              select
              label="Card type"
              value={brand}
              onChange={(event) => setBrand(event.target.value as CardBrand)}
              fullWidth
            >
              {BRANDS.map((option) => (
                <MenuItem key={option} value={option}>
                  {option}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label="Last four digits"
              required
              value={last4}
              onChange={(event) => setLast4(event.target.value.replace(/\D/g, '').slice(0, 4))}
              helperText="Four digits only. The server refuses anything longer rather than trimming it."
              fullWidth
            />
            <TextField
              label="Expiry"
              required
              placeholder="09/28"
              value={expiry}
              onChange={(event) => setExpiry(event.target.value)}
              fullWidth
            />
            <TextField
              label="Cardholder name"
              required
              value={holder}
              onChange={(event) => setHolder(event.target.value)}
              fullWidth
            />
            <Button
              size="small"
              startIcon={
                <MaterialSymbol
                  name={primary ? 'radio_button_checked' : 'radio_button_unchecked'}
                  size={18}
                />
              }
              onClick={() => setPrimary(!primary)}
              sx={{ alignSelf: 'flex-start' }}
            >
              Make default
            </Button>
            {add.isError ? (
              <Alert severity="error">
                {(add.error as { response?: { data?: { title?: string } } })?.response?.data
                  ?.title ?? 'We could not add that card.'}
              </Alert>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button color="inherit" onClick={() => setAdding(false)}>
            Cancel
          </Button>
          <Button
            variant="contained"
            disabled={last4.length !== 4 || !expiry.trim() || !holder.trim()}
            loading={add.isPending}
            onClick={() => add.mutate()}
          >
            Yes, save card
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={removing !== null}
        title="Remove this card?"
        description="Pending charges already authorised still go through. You can add the card again later."
        confirmLabel="Remove"
        destructive
        busy={remove.isPending}
        onConfirm={() => remove.mutate()}
        onCancel={() => setRemoving(null)}
      />
    </Stack>
  );
};

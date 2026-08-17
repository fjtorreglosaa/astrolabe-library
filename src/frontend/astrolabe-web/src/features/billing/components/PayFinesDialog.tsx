import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Stack,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { money } from '../../membership/planCopy';
import {
  getMyPaymentMethods,
  issueDeskPayment,
  payFines,
  type DeskPayment,
  type Fine,
  type PaymentReceipt,
} from '../api/billingApi';
import { DESK_CODE_CONFIRM, deskCodeNote } from '../billingCopy';

/**
 * The payment modal: choose fines, choose how to pay, confirm, done — the prototype's own three
 * steps.
 *
 * The two ways to pay are not two shades of the same thing. A card settles the debt now. A desk code
 * settles nothing: the member still owes the money until a librarian says they took it, and the
 * final step says so in as many words rather than showing a tick and the word "complete".
 */
export interface PayFinesDialogProps {
  open: boolean;
  fines: Fine[];
  onClose: () => void;
}

type Step = 'select' | 'confirm' | 'done';

export const PayFinesDialog = ({ open, fines, onClose }: PayFinesDialogProps) => {
  const queryClient = useQueryClient();
  const [step, setStep] = useState<Step>('select');
  const [picked, setPicked] = useState<string[]>([]);
  const [method, setMethod] = useState<string>('desk');
  const [receipt, setReceipt] = useState<PaymentReceipt | null>(null);
  const [deskCode, setDeskCode] = useState<DeskPayment | null>(null);

  const cards = useQuery({
    queryKey: ['billing', 'payment-methods'],
    queryFn: getMyPaymentMethods,
    enabled: open,
  });

  // Everything payable is selected when the modal opens, which is what the member almost always
  // wants; unticking is the rare case.
  useEffect(() => {
    if (open) {
      setPicked(fines.map((fine) => fine.id));
      setStep('select');
      setReceipt(null);
      setDeskCode(null);
    }
  }, [open, fines]);

  useEffect(() => {
    const primary = cards.data?.find((card) => card.isPrimary) ?? cards.data?.[0];
    setMethod(primary?.id ?? 'desk');
  }, [cards.data]);

  const selected = fines.filter((fine) => picked.includes(fine.id));
  const total = selected.reduce((sum, fine) => sum + fine.amountCents, 0);
  const isDesk = method === 'desk';

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['billing'] });

  const pay = useMutation({
    meta: { success: 'Payment received. Your receipt is in your statement.', silent: true },
    mutationFn: async () => {
      if (isDesk) {
        const code = await issueDeskPayment(picked);
        setDeskCode(code);
        return;
      }

      setReceipt(await payFines(picked, method));
    },
    onSuccess: async () => {
      await refresh();
      setStep('done');
    },
  });

  const close = () => {
    pay.reset();
    onClose();
  };

  return (
    <Dialog open={open} onClose={pay.isPending ? undefined : close} maxWidth="sm" fullWidth>
      {step === 'select' ? (
        <>
          <DialogTitle>Pay your fines</DialogTitle>
          <DialogContent>
            <Stack spacing={2.5}>
              <Stack spacing={1}>
                {fines.map((fine) => (
                  <Card key={fine.id} variant="outlined">
                    <CardActionArea
                      sx={{ p: 1.25 }}
                      onClick={() =>
                        setPicked((current) =>
                          current.includes(fine.id)
                            ? current.filter((id) => id !== fine.id)
                            : [...current, fine.id],
                        )
                      }
                    >
                      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                        <Checkbox checked={picked.includes(fine.id)} size="small" />
                        <Stack spacing={0.25} sx={{ flex: 1, minWidth: 0 }}>
                          <Typography variant="body2" noWrap>
                            {fine.bookTitle}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {fine.reason} · {fine.libraryName}
                          </Typography>
                        </Stack>
                        <Typography variant="body2">{money(fine.amountCents)}</Typography>
                      </Stack>
                    </CardActionArea>
                  </Card>
                ))}
              </Stack>

              <Divider />

              <Stack spacing={1}>
                <Typography variant="subtitle2">How would you like to pay?</Typography>

                {cards.data?.map((card) => (
                  <MethodOption
                    key={card.id}
                    icon="credit_card"
                    label={card.displayName}
                    note={`Expires ${card.expiryMonthYear}`}
                    selected={method === card.id}
                    onSelect={() => setMethod(card.id)}
                  />
                ))}

                <MethodOption
                  icon="storefront"
                  label="Pay at the library"
                  note="Cash or card at the desk · a librarian validates it"
                  selected={isDesk}
                  onSelect={() => setMethod('desk')}
                />
              </Stack>

              <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline' }}>
                <Typography variant="body2" color="text.secondary">
                  {selected.length} {selected.length === 1 ? 'fine' : 'fines'} selected
                </Typography>
                <Typography variant="h6">{money(total)}</Typography>
              </Stack>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={close} color="inherit">
              Cancel
            </Button>
            <Button
              variant="contained"
              disabled={selected.length === 0}
              onClick={() => setStep('confirm')}
            >
              Continue
            </Button>
          </DialogActions>
        </>
      ) : step === 'confirm' ? (
        <>
          <DialogTitle>{isDesk ? 'Generate a payment code?' : 'Confirm this payment?'}</DialogTitle>
          <DialogContent>
            <Stack spacing={2}>
              <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                <Typography variant="body2" color="text.secondary">
                  {isDesk ? 'To pay at the desk' : 'Charged now'}
                </Typography>
                <Typography variant="subtitle1">{money(total)}</Typography>
              </Stack>

              {isDesk ? (
                <Alert severity="info" icon={<MaterialSymbol name="storefront" size={20} />}>
                  {DESK_CODE_CONFIRM}
                </Alert>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  We charge {money(total)} to{' '}
                  {cards.data?.find((card) => card.id === method)?.displayName ?? 'your card'} and the
                  fines clear immediately.
                </Typography>
              )}

              {pay.isError ? (
                <Alert severity="error">
                  {(pay.error as { response?: { data?: { title?: string } } })?.response?.data
                    ?.title ?? 'We could not complete that payment.'}
                </Alert>
              ) : null}
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setStep('select')} color="inherit" disabled={pay.isPending}>
              Back
            </Button>
            <Button variant="contained" loading={pay.isPending} onClick={() => pay.mutate()}>
              {isDesk ? 'Generate code' : `Pay ${money(total)}`}
            </Button>
          </DialogActions>
        </>
      ) : (
        <>
          <DialogTitle>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <MaterialSymbol
                name={deskCode ? 'confirmation_number' : 'check_circle'}
                size={24}
                sx={{ color: deskCode ? 'warning.main' : 'success.main' }}
              />
              <Typography variant="h6">
                {deskCode ? 'Payment code ready' : 'Payment complete'}
              </Typography>
            </Stack>
          </DialogTitle>
          <DialogContent>
            <Stack spacing={2}>
              <Box
                component="code"
                sx={{
                  p: 1.5,
                  borderRadius: '8px',
                  bgcolor: 'action.hover',
                  fontSize: '1.25rem',
                  textAlign: 'center',
                }}
              >
                {deskCode?.code ?? receipt?.receipt}
              </Box>

              <Typography variant="body2" color="text.secondary">
                {deskCode
                  ? deskCodeNote(deskCode)
                  : `We charged ${money(receipt?.amountCents ?? 0)} to ${receipt?.paidWith}. `
                    + 'A receipt is on its way to your inbox.'}
              </Typography>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button variant="contained" onClick={close}>
              Done
            </Button>
          </DialogActions>
        </>
      )}
    </Dialog>
  );
};

const MethodOption = ({
  icon,
  label,
  note,
  selected,
  onSelect,
}: {
  icon: string;
  label: string;
  note: string;
  selected: boolean;
  onSelect: () => void;
}) => (
  <Card
    variant="outlined"
    sx={{ borderColor: selected ? 'primary.main' : 'divider', borderWidth: selected ? 2 : 1 }}
  >
    <CardActionArea sx={{ p: 1.25 }} onClick={onSelect}>
      <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
        <MaterialSymbol
          name={selected ? 'radio_button_checked' : 'radio_button_unchecked'}
          size={18}
          sx={{ color: selected ? 'primary.main' : 'text.secondary' }}
        />
        <MaterialSymbol name={icon} size={20} sx={{ color: 'text.secondary' }} />
        <Stack spacing={0.25}>
          <Typography variant="body2">{label}</Typography>
          <Typography variant="caption" color="text.secondary">
            {note}
          </Typography>
        </Stack>
      </Stack>
    </CardActionArea>
  </Card>
);

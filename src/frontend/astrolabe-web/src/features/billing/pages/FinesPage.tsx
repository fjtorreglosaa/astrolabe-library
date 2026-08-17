import {
  Box,
  Button,
  Chip,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState } from '../../../shared/components/StateViews';
import { TableSkeleton } from '../../../shared/components/TableSkeleton';
import { formatDate, money } from '../../membership/planCopy';
import { getMyFines, getMyLedger } from '../api/billingApi';
import {
  FINE_RULE_NOTE,
  FINE_STATUS_LABEL,
  LEDGER_KIND_LABEL,
  fineStatusTone,
  isPayableByCard,
} from '../billingCopy';
import { PayFinesDialog } from '../components/PayFinesDialog';
import { PaymentMethodsPanel } from '../../settings/components/PaymentMethodsPanel';

/**
 * Fines & payments — what the member owes, and the account statement behind it.
 *
 * Money held by a desk code is shown separately from money that can still be paid here. Folding the
 * two into one total would invite the member to pay a debt they have already promised to a counter,
 * and the API refuses that — so showing it as payable would only produce an error they did not earn.
 */
export const FinesPage = () => {
  const [paying, setPaying] = useState(false);

  const fines = useQuery({ queryKey: ['billing', 'fines'], queryFn: getMyFines });
  const ledger = useQuery({ queryKey: ['billing', 'ledger'], queryFn: () => getMyLedger(1, 20) });

  if (fines.isLoading) {
    return <TableSkeleton rows={4} label="Loading your account" />;
  }

  if (fines.isError || !fines.data) {
    return (
      <ErrorState description="We could not load your account." onRetry={() => void fines.refetch()} />
    );
  }

  const data = fines.data;
  const payable = data.fines.filter(isPayableByCard);

  return (
    // The prototype narrows this screen to 920px inside the shell's own column and centres it.
    // A balance and a short list of fines read badly across a full-width page: the eye has to
    // travel the whole way to pair a book with its amount.
    <Stack spacing={3} sx={{ maxWidth: 920, mx: 'auto', width: '100%' }}>
      <Stack spacing={0.5}>
        <Typography variant="h4">Fines &amp; payments</Typography>
      </Stack>

      <Paper variant="outlined" sx={{ p: 3, borderRadius: '12px' }}>
        <Stack
          direction="row"
          spacing={3}
          sx={{ alignItems: 'center', flexWrap: 'wrap', rowGap: 2 }}
        >
          <Stack spacing={0.75} sx={{ flex: 1, minWidth: 200 }}>
            <Typography variant="overline" color="text.secondary">
              Outstanding balance
            </Typography>
            {/* The prototype's 40px serif figure. It is the one number the page exists to show,
                and setting it at body scale made it compete with the rows beneath it. */}
            <Typography
              variant="h1"
              sx={{ fontSize: '2.5rem', lineHeight: 1 }}
              color={data.outstandingCents > 0 ? 'error.main' : 'text.primary'}
            >
              {money(data.outstandingCents)}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {FINE_RULE_NOTE}
            </Typography>
          </Stack>

          {payable.length > 0 ? (
            <Button
              variant="contained"
              size="large"
              startIcon={<MaterialSymbol name="payments" size={19} />}
              onClick={() => setPaying(true)}
            >
              Pay fines
            </Button>
          ) : (
            // A settled account is told so, rather than shown a disabled button. The prototype
            // swaps the control for the outcome, which is the difference between "you cannot do
            // this" and "there is nothing to do".
            <Chip
              color="success"
              variant="outlined"
              icon={<MaterialSymbol name="check_circle" size={19} />}
              label="All settled"
              sx={{ height: 44, px: 1.5, fontWeight: 600 }}
            />
          )}
        </Stack>
      </Paper>

      {data.openDeskPayments.map((payment) => (
        <Paper
          key={payment.id}
          variant="outlined"
          sx={{
            p: 2.25,
            borderRadius: '12px',
            borderColor: 'rgba(224,166,60,.5)',
            bgcolor: 'rgba(224,166,60,.12)',
          }}
        >
          <Stack direction="row" spacing={1.75} sx={{ alignItems: 'flex-start' }}>
            <MaterialSymbol name="storefront" size={22} sx={{ color: '#8A6A28', mt: 0.25 }} />
            <Stack spacing={0.5} sx={{ minWidth: 0 }}>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                {money(payment.amountCents)} waiting to be paid at the desk
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Show code{' '}
                <Box component="strong" sx={{ color: 'text.primary' }}>
                  {payment.code}
                </Box>{' '}
                at {payment.libraryName} before {formatDate(payment.expiresAt)}. A librarian
                validates it and these fines clear automatically.
              </Typography>
              <Box>
                <Button
                  size="small"
                  variant="outlined"
                  startIcon={<MaterialSymbol name="content_copy" size={15} />}
                  onClick={() => void navigator.clipboard?.writeText(payment.code)}
                  sx={{ mt: 0.5, color: '#8A6A28', borderColor: 'rgba(138,106,40,.5)' }}
                >
                  Copy code
                </Button>
              </Box>
            </Stack>
          </Stack>
        </Paper>
      ))}

      <Paper variant="outlined" sx={{ borderRadius: '12px', overflow: 'hidden' }}>
        <Typography
          variant="h6"
          sx={{ px: 2.5, py: 2.25, borderBottom: 1, borderColor: 'divider' }}
        >
          Fines
        </Typography>

        {data.fines.length === 0 ? (
          <EmptyState
            title="No fines"
            description="Return your books on time and this page stays empty."
          />
        ) : (
          <Stack>
            {/* Rows, not a table. Six columns of mostly-empty cells is what made this screen look
                like an administration report rather than a member's own account. */}
            {data.fines.map((fine, index) => (
              <Stack
                key={fine.id}
                direction="row"
                spacing={2}
                sx={{
                  px: 2.5,
                  py: 2,
                  alignItems: 'center',
                  flexWrap: 'wrap',
                  rowGap: 1,
                  borderBottom: index < data.fines.length - 1 ? 1 : 0,
                  borderColor: 'divider',
                }}
              >
                <Stack spacing={0.375} sx={{ flex: 1, minWidth: 180 }}>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {fine.bookTitle}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {fine.reason} · {fine.libraryName} · charged {formatDate(fine.assessedAt)}
                  </Typography>
                </Stack>

                <Chip
                  size="small"
                  color={fineStatusTone(fine)}
                  variant={fine.status === 'Paid' ? 'filled' : 'outlined'}
                  label={FINE_STATUS_LABEL[fine.status]}
                />

                <Typography
                  variant="h6"
                  sx={{ minWidth: 70, textAlign: 'right', fontSize: '1.25rem' }}
                >
                  {money(fine.amountCents)}
                </Typography>
              </Stack>
            ))}
          </Stack>
        )}
      </Paper>

      <PaymentMethodsPanel />

      <Paper variant="outlined" sx={{ borderRadius: '12px', overflow: 'hidden' }}>
        <Typography
          variant="h6"
          sx={{ px: 2.5, py: 2.25, borderBottom: 1, borderColor: 'divider' }}
        >
          Payment history
        </Typography>

        {!ledger.data || ledger.data.items.length === 0 ? (
          <EmptyState
            title="Nothing has moved yet"
            description="Charges and payments appear here as they happen."
          />
        ) : (
          <Box sx={{ overflowX: 'auto' }}>
            {/* The prototype's table, minus two columns it has and this system does not: the
                payment method used, and a receipt number. Neither exists on a ledger entry, and
                inventing them here would put a figure on screen with nothing behind it. Raised as
                GLOBAL-029 rather than faked. */}
            <Table size="small" sx={{ minWidth: 520 }}>
              <TableHead>
                <TableRow>
                  <TableCell>Date</TableCell>
                  <TableCell>Description</TableCell>
                  <TableCell>Kind</TableCell>
                  <TableCell align="right">Amount</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {ledger.data.items.map((entry) => (
                  <TableRow key={entry.id} hover>
                    <TableCell sx={{ whiteSpace: 'nowrap', color: 'text.secondary' }}>
                      {formatDate(entry.occurredAt)}
                    </TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>{entry.description}</TableCell>
                    <TableCell sx={{ color: 'text.secondary', whiteSpace: 'nowrap' }}>
                      {LEDGER_KIND_LABEL[entry.kind]}
                    </TableCell>
                    <TableCell
                      align="right"
                      // The amount arrives signed — a charge is negative — so the sign is the
                      // server's answer rather than this table guessing which kinds are debits.
                      sx={{
                        whiteSpace: 'nowrap',
                        fontWeight: 600,
                        color: entry.amountCents < 0 ? 'error.main' : 'success.main',
                      }}
                    >
                      {entry.amountCents < 0 ? '−' : '+'}
                      {money(Math.abs(entry.amountCents))}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>
        )}

        {/* A running balance the prototype does not show, kept because it is the one number that
            answers "so where do I stand?" without adding the column up by hand. */}
        {ledger.data && ledger.data.items.length > 0 ? (
          <Stack
            direction="row"
            sx={{
              px: 2.5,
              py: 2,
              justifyContent: 'space-between',
              borderTop: 1,
              borderColor: 'divider',
            }}
          >
            <Typography variant="subtitle2">Balance</Typography>
            <Typography
              variant="subtitle2"
              color={data.balanceCents < 0 ? 'error.main' : 'success.main'}
            >
              {money(data.balanceCents)}
            </Typography>
          </Stack>
        ) : null}
      </Paper>

      <PayFinesDialog open={paying} fines={payable} onClose={() => setPaying(false)} />
    </Stack>
  );
};

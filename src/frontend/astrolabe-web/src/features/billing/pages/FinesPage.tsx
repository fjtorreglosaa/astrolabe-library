import {
  Alert,
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
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { formatDate, money } from '../../membership/planCopy';
import { getMyFines, getMyLedger } from '../api/billingApi';
import {
  FINE_RULE_NOTE,
  FINE_STATUS_LABEL,
  LEDGER_KIND_LABEL,
  fineStatusTone,
  isPayableByCard,
  outstandingNote,
} from '../billingCopy';
import { PayFinesDialog } from '../components/PayFinesDialog';

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
    return <LoadingState label="Loading your account…" />;
  }

  if (fines.isError || !fines.data) {
    return (
      <ErrorState description="We could not load your account." onRetry={() => void fines.refetch()} />
    );
  }

  const data = fines.data;
  const payable = data.fines.filter(isPayableByCard);

  return (
    <Stack spacing={3}>
      <Stack spacing={0.5}>
        <Typography variant="h4">Fines &amp; payments</Typography>
        <Typography variant="body2" color="text.secondary">
          {FINE_RULE_NOTE}
        </Typography>
      </Stack>

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ alignItems: { sm: 'center' }, justifyContent: 'space-between' }}
        >
          <Stack spacing={0.5}>
            <Typography variant="overline" color="text.secondary">
              Outstanding
            </Typography>
            <Typography
              variant="h3"
              color={data.outstandingCents > 0 ? 'error.main' : 'success.main'}
            >
              {money(data.outstandingCents)}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {outstandingNote(data.outstandingCents, payable.length)}
            </Typography>
          </Stack>

          <Button
            variant="contained"
            size="large"
            disabled={payable.length === 0}
            onClick={() => setPaying(true)}
          >
            Pay now
          </Button>
        </Stack>
      </Paper>

      {data.awaitingValidationCents > 0 ? (
        <Alert severity="warning" icon={<MaterialSymbol name="storefront" size={20} />}>
          {money(data.awaitingValidationCents)} is waiting to be paid at a library desk. It is still
          owed — a librarian clears it when they take the money.
        </Alert>
      ) : null}

      {data.openDeskPayments.map((payment) => (
        <Paper key={payment.id} variant="outlined" sx={{ p: 2 }}>
          <Stack direction="row" spacing={2} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
            <MaterialSymbol name="confirmation_number" size={22} sx={{ color: 'warning.main' }} />
            <Stack spacing={0.25} sx={{ flex: 1, minWidth: 0 }}>
              <Typography variant="subtitle2">{payment.code}</Typography>
              <Typography variant="caption" color="text.secondary">
                {money(payment.amountCents)} at {payment.libraryName} · expires{' '}
                {formatDate(payment.expiresAt)}
              </Typography>
            </Stack>
            <Chip size="small" color="warning" variant="outlined" label="Awaiting the desk" />
          </Stack>
        </Paper>
      ))}

      <Paper variant="outlined">
        {data.fines.length === 0 ? (
          <EmptyState
            title="No fines"
            description="Return your books on time and this page stays empty."
          />
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Book</TableCell>
                <TableCell>Reason</TableCell>
                <TableCell>Library</TableCell>
                <TableCell>Assessed</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Amount</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data.fines.map((fine) => (
                <TableRow key={fine.id} hover>
                  <TableCell>{fine.bookTitle}</TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {fine.reason}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {fine.libraryName}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {formatDate(fine.assessedAt)}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      color={fineStatusTone(fine)}
                      variant={fine.status === 'Paid' ? 'filled' : 'outlined'}
                      label={FINE_STATUS_LABEL[fine.status]}
                    />
                  </TableCell>
                  <TableCell align="right">{money(fine.amountCents)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Paper>

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack spacing={2}>
          <Stack spacing={0.5}>
            <Typography variant="h6">Account statement</Typography>
            <Typography variant="body2" color="text.secondary">
              Every movement, kept whole. A payment never removes the charge it answers.
            </Typography>
          </Stack>

          {ledger.data && ledger.data.items.length > 0 ? (
            <Stack spacing={1}>
              {ledger.data.items.map((entry) => (
                <Stack
                  key={entry.id}
                  direction="row"
                  spacing={2}
                  sx={{ justifyContent: 'space-between', alignItems: 'center' }}
                >
                  <Stack spacing={0.25} sx={{ minWidth: 0 }}>
                    <Typography variant="body2" noWrap>
                      {entry.description}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {formatDate(entry.occurredAt)} · {LEDGER_KIND_LABEL[entry.kind]}
                    </Typography>
                  </Stack>
                  <Typography
                    variant="body2"
                    color={entry.amountCents < 0 ? 'error.main' : 'success.main'}
                  >
                    {entry.amountCents < 0 ? '−' : '+'}
                    {money(Math.abs(entry.amountCents))}
                  </Typography>
                </Stack>
              ))}

              <Box sx={{ pt: 1, borderTop: 1, borderColor: 'divider' }}>
                <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                  <Typography variant="subtitle2">Balance</Typography>
                  <Typography
                    variant="subtitle2"
                    color={data.balanceCents < 0 ? 'error.main' : 'success.main'}
                  >
                    {money(data.balanceCents)}
                  </Typography>
                </Stack>
              </Box>
            </Stack>
          ) : (
            <Typography variant="body2" color="text.secondary">
              Nothing has moved on your account yet.
            </Typography>
          )}
        </Stack>
      </Paper>

      <PayFinesDialog open={paying} fines={payable} onClose={() => setPaying(false)} />
    </Stack>
  );
};

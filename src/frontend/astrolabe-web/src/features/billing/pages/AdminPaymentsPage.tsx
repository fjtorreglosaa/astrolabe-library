import {
  Alert,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { formatDate, money } from '../../membership/planCopy';
import {
  getDeskQueue,
  rejectDeskPayment,
  validateDeskPayment,
  type DeskPayment,
  type DeskPaymentStatus,
} from '../api/billingApi';
import { DESK_STATUS_FILTERS } from '../billingCopy';

/**
 * Manual payments — the desk queue.
 *
 * Validating means "I have the money in my hand". It is the only act that clears a member's debt, so
 * it asks for confirmation rather than firing on a single click, and rejecting demands a reason —
 * a rejection puts the debt back on somebody's account.
 */
export const AdminPaymentsPage = () => {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<DeskPaymentStatus>('Pending');
  const [acting, setActing] = useState<{ payment: DeskPayment; kind: 'validate' | 'reject' } | null>(
    null,
  );
  const [reason, setReason] = useState('');

  const queue = useQuery({
    queryKey: ['billing', 'desk-queue', status],
    queryFn: () => getDeskQueue(status),
  });

  const act = useMutation({
    mutationFn: async () => {
      if (!acting) {
        return;
      }

      if (acting.kind === 'validate') {
        await validateDeskPayment(acting.payment.code);
        return;
      }

      await rejectDeskPayment(acting.payment.code, reason);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['billing'] });
      close();
    },
  });

  const close = () => {
    act.reset();
    setReason('');
    setActing(null);
  };

  return (
    <Stack spacing={3}>
      <Stack spacing={0.5}>
        <Typography variant="h4">Manual payments</Typography>
        <Typography variant="body2" color="text.secondary">
          Codes members brought to your desk. Validate only once the money is in your hand.
        </Typography>
      </Stack>

      <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
        {DESK_STATUS_FILTERS.map((filter) => (
          <Chip
            key={filter.value}
            label={filter.label}
            color={status === filter.value ? 'primary' : 'default'}
            variant={status === filter.value ? 'filled' : 'outlined'}
            onClick={() => setStatus(filter.value)}
          />
        ))}
      </Stack>

      {queue.isLoading ? (
        <LoadingState label="Loading the queue…" />
      ) : queue.isError || !queue.data ? (
        <ErrorState description="We could not load the queue." onRetry={() => void queue.refetch()} />
      ) : queue.data.items.length === 0 ? (
        <EmptyState title="Nothing here" description="No payment codes in this state." />
      ) : (
        <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Code</TableCell>
                <TableCell>Member</TableCell>
                <TableCell>Concept</TableCell>
                <TableCell>Library</TableCell>
                <TableCell>Issued</TableCell>
                <TableCell align="right">Amount</TableCell>
                <TableCell align="right">Action</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {queue.data.items.map((payment) => (
                <TableRow key={payment.id} hover>
                  <TableCell>
                    <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                      {payment.code}
                    </Typography>
                  </TableCell>
                  <TableCell>{payment.memberName}</TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {payment.concept}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {payment.libraryName}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Stack spacing={0.25}>
                      <Typography variant="body2" color="text.secondary">
                        {formatDate(payment.issuedAt)}
                      </Typography>
                      {/* Expiry is decided by the server. A code that has run out cannot be
                          validated, so the desk must see that before the member is asked to pay. */}
                      {payment.isExpired ? (
                        <Chip size="small" color="error" variant="outlined" label="Expired" />
                      ) : null}
                    </Stack>
                  </TableCell>
                  <TableCell align="right">{money(payment.amountCents)}</TableCell>
                  <TableCell align="right">
                    {payment.status === 'Pending' && !payment.isExpired ? (
                      <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                        <Button
                          size="small"
                          variant="contained"
                          onClick={() => setActing({ payment, kind: 'validate' })}
                        >
                          Validate
                        </Button>
                        <Button
                          size="small"
                          color="error"
                          onClick={() => setActing({ payment, kind: 'reject' })}
                        >
                          Reject
                        </Button>
                      </Stack>
                    ) : (
                      <Typography variant="caption" color="text.secondary">
                        {payment.rejectionReason ?? payment.status}
                      </Typography>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}

      <Dialog open={acting !== null} onClose={act.isPending ? undefined : close} maxWidth="xs" fullWidth>
        <DialogTitle>
          {acting?.kind === 'validate' ? 'Validate this payment?' : 'Reject this payment?'}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              {acting?.kind === 'validate'
                ? `${money(acting.payment.amountCents)} from ${acting.payment.memberName}. `
                  + 'Their fines clear immediately, so only confirm once you have the money.'
                : 'The fines go back to unpaid and the member can pay again. Say why, so they know.'}
            </Typography>

            {acting?.kind === 'reject' ? (
              <TextField
                label="Reason"
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                fullWidth
                autoFocus
                multiline
                minRows={2}
              />
            ) : null}

            {act.isError ? (
              <Alert severity="error">
                {(act.error as { response?: { data?: { title?: string } } })?.response?.data?.title ??
                  'We could not complete that.'}
              </Alert>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={close} color="inherit" disabled={act.isPending}>
            Cancel
          </Button>
          <Button
            variant="contained"
            color={acting?.kind === 'reject' ? 'error' : 'primary'}
            disabled={acting?.kind === 'reject' && reason.trim().length === 0}
            loading={act.isPending}
            onClick={() => act.mutate()}
          >
            {acting?.kind === 'validate' ? 'Validate payment' : 'Reject payment'}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
};

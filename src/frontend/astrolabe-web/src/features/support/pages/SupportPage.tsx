import {
  Alert,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Pagination,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState } from '../../../shared/components/StateViews';
import { ListRowsSkeleton } from '../../../shared/components/ListRowsSkeleton';
import { formatDate } from '../../membership/planCopy';
import { useAuth } from '../../auth/components/AuthProvider';
import { getAdministeredLibraries, getLibraries } from '../../network/api/networkApi';
import {
  openTicket,
  searchTickets,
  type TicketCategory,
  type TicketStatus,
} from '../api/supportApi';
import { CATEGORY_LABEL, STATUS_COLOR, STATUS_FILTERS, STATUS_ICON, STATUS_LABEL } from '../supportCopy';
import { TicketThread } from '../components/TicketThread';

const PAGE_SIZE = 20;

/**
 * Support tickets. One screen for both audiences.
 *
 * <p>
 * A member sees their own; staff see the queue for the libraries they administer. Neither is decided
 * here — the same endpoint answers differently by role, which is what keeps BR-SUP-004 and
 * BR-SUP-010 in one place instead of two.
 * </p>
 */
export const SupportPage = () => {
  const queryClient = useQueryClient();
  const { role } = useAuth();
  const isStaff = role === 'Admin' || role === 'SuperAdmin';

  const [status, setStatus] = useState<TicketStatus | 'All'>('All');
  const [page, setPage] = useState(1);
  const [openId, setOpenId] = useState<string | null>(null);
  const [composing, setComposing] = useState(false);
  const [subject, setSubject] = useState('');
  const [body, setBody] = useState('');
  const [category, setCategory] = useState<TicketCategory>('PaymentsAndFines');
  const [libraryId, setLibraryId] = useState('');

  const tickets = useQuery({
    queryKey: ['support', 'tickets', status, page],
    queryFn: () =>
      searchTickets({
        status: status === 'All' ? undefined : status,
        page,
        pageSize: PAGE_SIZE,
      }),
  });

  // A member picks any library; staff would only ever open one for their own.
  const libraries = useQuery({
    queryKey: ['network', isStaff ? 'administered-libraries' : 'libraries'],
    queryFn: () => (isStaff ? getAdministeredLibraries() : getLibraries()),
    enabled: composing,
  });

  const create = useMutation({
    meta: { silent: true },
    mutationFn: () => openTicket({ subject, body, category, libraryId }),
    onSuccess: async (ticket) => {
      setComposing(false);
      setSubject('');
      setBody('');
      setLibraryId('');
      await queryClient.invalidateQueries({ queryKey: ['support'] });
      setOpenId(ticket.id);
    },
  });

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'flex-start' } }}
      >
        <Stack spacing={0.5}>
          <Typography variant="h4">{isStaff ? 'Support tickets' : 'Help & support'}</Typography>
          <Typography variant="body2" color="text.secondary">
            {isStaff
              ? 'Tickets for the libraries you administer.'
              : 'Ask us anything. We answer here, and you will be notified.'}
          </Typography>
        </Stack>
        {!isStaff ? (
          <Button
            variant="contained"
            startIcon={<MaterialSymbol name="add_comment" size={20} />}
            onClick={() => setComposing(true)}
          >
            New ticket
          </Button>
        ) : null}
      </Stack>

      <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
        {STATUS_FILTERS.map((option) => (
          <Chip
            key={option}
            size="small"
            variant={status === option ? 'filled' : 'outlined'}
            color={status === option ? 'primary' : 'default'}
            label={option === 'All' ? 'All' : STATUS_LABEL[option]}
            onClick={() => {
              setStatus(option);
              setPage(1);
            }}
          />
        ))}
      </Stack>

      {tickets.isLoading ? (
        <ListRowsSkeleton rows={4} label="Loading tickets" />
      ) : tickets.isError || !tickets.data ? (
        <ErrorState description="We could not load your tickets." onRetry={() => void tickets.refetch()} />
      ) : tickets.data.items.length === 0 ? (
        <EmptyState
          title={isStaff ? 'Nothing waiting' : 'No tickets yet'}
          description={
            isStaff
              ? 'No ticket is open for the libraries you administer.'
              : 'If something goes wrong, open a ticket and we will pick it up.'
          }
        />
      ) : (
        <>
          <Stack spacing={1.5}>
            {tickets.data.items.map((ticket) => (
              <Paper
                key={ticket.id}
                variant="outlined"
                sx={{ p: 2, cursor: 'pointer' }}
                onClick={() => setOpenId(ticket.id)}
              >
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  spacing={1}
                  sx={{ justifyContent: 'space-between' }}
                >
                  <Stack spacing={0.25} sx={{ minWidth: 0 }}>
                    <Typography variant="subtitle2" noWrap>
                      {ticket.reference} · {ticket.subject}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {ticket.category} · {ticket.libraryName}
                      {isStaff ? ` · ${ticket.memberName}` : ''} · {formatDate(ticket.updatedAt)}
                    </Typography>
                  </Stack>
                  <Chip
                    size="small"
                    variant="outlined"
                    color={STATUS_COLOR[ticket.status.replace(' ', '') as TicketStatus] ?? 'default'}
                    icon={
                      <MaterialSymbol
                        name={STATUS_ICON[ticket.status.replace(' ', '') as TicketStatus] ?? 'help'}
                        size={16}
                      />
                    }
                    label={ticket.status}
                  />
                </Stack>
              </Paper>
            ))}
          </Stack>

          {tickets.data.totalPages > 1 ? (
            <Stack sx={{ alignItems: 'center' }}>
              <Pagination
                count={tickets.data.totalPages}
                page={tickets.data.page}
                onChange={(_event, next) => setPage(next)}
                color="primary"
              />
            </Stack>
          ) : null}
        </>
      )}

      <TicketThread ticketId={openId} isStaff={isStaff} onClose={() => setOpenId(null)} />

      <Dialog open={composing} onClose={() => setComposing(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Open a ticket</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              label="Subject"
              required
              value={subject}
              onChange={(event) => setSubject(event.target.value)}
              fullWidth
            />
            <TextField
              select
              label="What is it about?"
              value={category}
              onChange={(event) => setCategory(event.target.value as TicketCategory)}
              fullWidth
            >
              {(Object.keys(CATEGORY_LABEL) as TicketCategory[]).map((option) => (
                <MenuItem key={option} value={option}>
                  {CATEGORY_LABEL[option]}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              select
              label="Which library?"
              required
              value={libraryId}
              onChange={(event) => setLibraryId(event.target.value)}
              helperText="It routes the ticket to staff who can act on it."
              fullWidth
            >
              {(libraries.data ?? [])
                .filter((library) => library.isActive)
                .map((library) => (
                  <MenuItem key={library.id} value={library.id}>
                    {library.name}
                  </MenuItem>
                ))}
            </TextField>
            <TextField
              label="What happened?"
              required
              multiline
              minRows={4}
              value={body}
              onChange={(event) => setBody(event.target.value)}
              fullWidth
            />
            {create.isError ? (
              <Alert severity="error">
                {(create.error as { response?: { data?: { title?: string } } })?.response?.data
                  ?.title ?? 'We could not open that ticket.'}
              </Alert>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button color="inherit" onClick={() => setComposing(false)}>
            Cancel
          </Button>
          <Button
            variant="contained"
            disabled={!subject.trim() || !body.trim() || !libraryId}
            loading={create.isPending}
            onClick={() => create.mutate()}
          >
            Open ticket
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
};

import {
  Alert,
  Box,
  Button,
  Chip,
  Divider,
  Drawer,
  Rating,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { formatDate } from '../../membership/planCopy';
import {
  getTicket,
  rateTicket,
  replyToTicket,
  transitionTicket,
  type TicketMessage,
  type TicketTransition,
} from '../api/supportApi';
import { RATING_NOTE, RESOLVED_NOTE, STATUS_COLOR, STATUS_ICON } from '../supportCopy';

/**
 * One ticket and its conversation.
 *
 * <p>
 * What the reader may do is the server's answer, not this component's: `canReply` and `canRate`
 * arrive on the DTO because both depend on the ticket's status <em>and</em> on who is asking, and
 * deciding here would be a second copy of BR-SUP-005 and BR-SUP-011 — the copy that drifts.
 * </p>
 * <p>
 * The staff transitions are offered only to staff, and the server refuses them regardless. The
 * screen is a convenience; the guard is `BR-SUP-010`.
 * </p>
 */
export interface TicketThreadProps {
  ticketId: string | null;
  isStaff: boolean;
  onClose: () => void;
}

export const TicketThread = ({ ticketId, isStaff, onClose }: TicketThreadProps) => {
  const queryClient = useQueryClient();
  const [reply, setReply] = useState('');
  const [stars, setStars] = useState<number | null>(null);
  const [review, setReview] = useState('');

  const ticket = useQuery({
    queryKey: ['support', 'ticket', ticketId],
    queryFn: () => getTicket(ticketId!),
    enabled: ticketId !== null,
  });

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['support'] });

  const send = useMutation({
    mutationFn: () => replyToTicket(ticketId!, reply),
    onSuccess: async () => {
      setReply('');
      await refresh();
    },
  });

  const move = useMutation({
    mutationFn: (transition: TicketTransition) => transitionTicket(ticketId!, transition),
    onSuccess: refresh,
  });

  const rate = useMutation({
    mutationFn: () => rateTicket(ticketId!, stars ?? 0, review.trim() || null),
    onSuccess: async () => {
      setStars(null);
      setReview('');
      await refresh();
    },
  });

  const data = ticket.data;
  const failure = send.error ?? move.error ?? rate.error;

  return (
    <Drawer anchor="right" open={ticketId !== null} onClose={onClose}>
      <Stack sx={{ width: { xs: '100vw', sm: 520 }, p: 3 }} spacing={2.5}>
        {ticket.isLoading || !data ? (
          ticket.isError ? (
            <ErrorState
              description="We could not load that ticket."
              onRetry={() => void ticket.refetch()}
            />
          ) : (
            <LoadingState label="Loading the conversation…" />
          )
        ) : (
          <>
            <Stack spacing={0.5}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <Typography variant="overline" color="text.secondary">
                  {data.reference}
                </Typography>
                <Chip
                  size="small"
                  variant="outlined"
                  color={STATUS_COLOR[data.status.replace(' ', '') as 'Created'] ?? 'default'}
                  icon={
                    <MaterialSymbol
                      name={STATUS_ICON[data.status.replace(' ', '') as 'Created'] ?? 'help'}
                      size={16}
                    />
                  }
                  label={data.status}
                />
              </Stack>
              <Typography variant="h6">{data.subject}</Typography>
              <Typography variant="caption" color="text.secondary">
                {data.category} · {data.libraryName}
                {data.agentName ? ` · handled by ${data.agentName}` : ' · unassigned'}
              </Typography>
            </Stack>

            <Divider />

            <Stack spacing={1.5} sx={{ maxHeight: 380, overflowY: 'auto' }}>
              {data.messages.map((message) => (
                <Message key={message.id} message={message} />
              ))}
            </Stack>

            {failure ? (
              <Alert severity="error">
                {(failure as { response?: { data?: { title?: string } } })?.response?.data?.title ??
                  'We could not do that.'}
              </Alert>
            ) : null}

            {data.canReply ? (
              <Stack spacing={1}>
                <TextField
                  multiline
                  minRows={3}
                  placeholder="Write a reply…"
                  value={reply}
                  onChange={(event) => setReply(event.target.value)}
                  fullWidth
                />
                <Button
                  variant="contained"
                  disabled={!reply.trim()}
                  loading={send.isPending}
                  onClick={() => send.mutate()}
                >
                  Send
                </Button>
              </Stack>
            ) : (
              <Alert severity="info" icon={<MaterialSymbol name="lock" size={20} />}>
                {RESOLVED_NOTE}
              </Alert>
            )}

            {/* BR-SUP-005. Only the member, only once resolved — and the server said so. */}
            {data.canRate ? (
              <Stack spacing={1}>
                <Divider />
                <Typography variant="subtitle2">How did we do?</Typography>
                <Rating value={stars ?? data.rating ?? 0} onChange={(_e, v) => setStars(v)} />
                <TextField
                  size="small"
                  placeholder="A few words (optional)"
                  value={review}
                  onChange={(event) => setReview(event.target.value)}
                  fullWidth
                />
                <Typography variant="caption" color="text.secondary">
                  {RATING_NOTE}
                </Typography>
                <Button
                  variant="outlined"
                  disabled={!stars}
                  loading={rate.isPending}
                  onClick={() => rate.mutate()}
                >
                  {data.rating ? 'Update my rating' : 'Send my rating'}
                </Button>
              </Stack>
            ) : data.rating ? (
              <Stack spacing={0.5}>
                <Divider />
                <Rating value={data.rating} readOnly size="small" />
                {data.review ? (
                  <Typography variant="body2" color="text.secondary">
                    “{data.review}”
                  </Typography>
                ) : null}
              </Stack>
            ) : null}

            {isStaff ? (
              <Stack direction="row" spacing={1} sx={{ pt: 1 }}>
                {!data.agentName ? (
                  <Button size="small" variant="contained" onClick={() => move.mutate('Assign')}>
                    Take this ticket
                  </Button>
                ) : null}
                {data.status !== 'Resolved' && data.agentName ? (
                  <Button size="small" onClick={() => move.mutate('Resolve')}>
                    Mark resolved
                  </Button>
                ) : null}
                {data.status === 'Resolved' ? (
                  <Button size="small" color="warning" onClick={() => move.mutate('Reopen')}>
                    Reopen
                  </Button>
                ) : null}
              </Stack>
            ) : null}
          </>
        )}
      </Stack>
    </Drawer>
  );
};

const Message = ({ message }: { message: TicketMessage }) => {
  const fromAgent = message.author === 'Agent';

  return (
    <Box
      sx={{
        alignSelf: fromAgent ? 'flex-start' : 'flex-end',
        maxWidth: '85%',
        p: 1.5,
        borderRadius: 2,
        // Two sides of a conversation, told apart at a glance rather than by reading the name.
        bgcolor: fromAgent ? 'action.hover' : 'primary.main',
        color: fromAgent ? 'text.primary' : 'primary.contrastText',
      }}
    >
      <Typography variant="caption" sx={{ opacity: 0.8 }}>
        {message.authorName} · {formatDate(message.writtenAt)}
      </Typography>
      <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
        {message.text}
      </Typography>
    </Box>
  );
};

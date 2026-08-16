import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import {
  Button,
  Chip,
  Divider,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog';
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { useAuth } from '../../auth/components/AuthProvider';
import { getMySessions, revokeSession, revokeSessions, RevocationScope, type Session } from '../api/sessionsApi';

/**
 * Devices and sessions.
 *
 * **This screen does not exist in the approved prototype** (`BLOCK-004`). It is built in the
 * prototype's visual language and needs a design review before the domain is accepted.
 *
 * It shows only the caller's own sessions: the API takes no parameter for whose sessions to act on,
 * so BR-IDN-025 cannot be bypassed from here or anywhere else.
 */
export const DevicesAndSessionsPage = () => {
  const queryClient = useQueryClient();
  const { signOut } = useAuth();
  const [pending, setPending] = useState<{ kind: 'one'; session: Session } | { kind: 'others' } | { kind: 'all' } | null>(null);

  const sessions = useQuery({ queryKey: ['sessions'], queryFn: getMySessions });

  const revoke = useMutation({
    mutationFn: async (target: NonNullable<typeof pending>) => {
      if (target.kind === 'one') {
        return revokeSession(target.session.id);
      }

      return revokeSessions(
        target.kind === 'others' ? RevocationScope.AllOthers : RevocationScope.All,
      );
    },
    onSuccess: async (_result, target) => {
      setPending(null);

      // Ending every session ends this one too, so the only correct next state is signed out.
      if (target.kind === 'all') {
        await signOut();
        return;
      }

      await queryClient.invalidateQueries({ queryKey: ['sessions'] });
    },
  });

  if (sessions.isLoading) {
    return <LoadingState label="Loading your devices…" />;
  }

  if (sessions.isError) {
    return <ErrorState description="We could not load your devices." onRetry={() => sessions.refetch()} />;
  }

  const live = sessions.data ?? [];
  const others = live.filter((session) => !session.isCurrent);

  return (
    <Stack spacing={3}>
      <Stack spacing={0.5}>
        <Typography variant="h4">Devices and sessions</Typography>
        <Typography variant="body2" color="text.secondary">
          Everywhere your account is signed in. Ending a session takes effect immediately.
        </Typography>
      </Stack>

      <Paper variant="outlined">
        {live.length === 0 ? (
          <EmptyState title="No active sessions" description="Sign in on a device and it will appear here." />
        ) : (
          <List disablePadding>
            {live.map((session, index) => (
              <ListItem
                key={session.id}
                divider={index < live.length - 1}
                secondaryAction={
                  session.isCurrent ? (
                    <Chip size="small" label="This device" color="primary" />
                  ) : (
                    <Button
                      color="error"
                      size="small"
                      onClick={() => setPending({ kind: 'one', session })}
                    >
                      Sign out
                    </Button>
                  )
                }
              >
                <ListItemIcon>{deviceIcon(session.deviceType)}</ListItemIcon>
                <ListItemText
                  primary={session.deviceName}
                  secondary={`${session.approximateLocation ?? session.ipAddress} · last active ${formatRelative(session.lastSeenAt)}`}
                />
              </ListItem>
            ))}
          </List>
        )}
      </Paper>

      <Divider />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <Button
          variant="outlined"
          color="error"
          disabled={others.length === 0}
          onClick={() => setPending({ kind: 'others' })}
        >
          Sign out everywhere else{others.length > 0 ? ` (${others.length})` : ''}
        </Button>
        <Button variant="contained" color="error" onClick={() => setPending({ kind: 'all' })}>
          Sign out everywhere
        </Button>
      </Stack>

      <ConfirmDialog
        open={pending !== null}
        title={confirmTitle(pending)}
        description={confirmDescription(pending)}
        confirmLabel="Sign out"
        destructive
        busy={revoke.isPending}
        onConfirm={() => pending && revoke.mutate(pending)}
        onCancel={() => setPending(null)}
      />
    </Stack>
  );
};

const deviceIcon = (deviceType: number) => {
  switch (deviceType) {
    case 2:
      return <MaterialSymbol name="smartphone" size={20} />;
    case 3:
      return <MaterialSymbol name="tablet" size={20} />;
    case 1:
    case 4:
      return <MaterialSymbol name="computer" size={20} />;
    default:
      return <MaterialSymbol name="devices" size={20} />;
  }
};

const confirmTitle = (pending: { kind: string } | null) => {
  switch (pending?.kind) {
    case 'others':
      return 'Sign out everywhere else?';
    case 'all':
      return 'Sign out everywhere?';
    default:
      return 'Sign out this device?';
  }
};

const confirmDescription = (pending: { kind: string } | null) => {
  switch (pending?.kind) {
    case 'others':
      return 'Every other device will have to sign in again. This one stays signed in.';
    case 'all':
      return 'Every device signs out, including this one. You will need to sign in again.';
    default:
      return 'That device will have to sign in again. Anything it was doing stops immediately.';
  }
};

const formatRelative = (iso: string): string => {
  const minutes = Math.round((Date.now() - new Date(iso).getTime()) / 60_000);

  if (minutes < 2) return 'just now';
  if (minutes < 60) return `${minutes} minutes ago`;

  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours} hour${hours === 1 ? '' : 's'} ago`;

  const days = Math.round(hours / 24);
  return `${days} day${days === 1 ? '' : 's'} ago`;
};

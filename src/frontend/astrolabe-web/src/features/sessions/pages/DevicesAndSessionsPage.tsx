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
import {
  getMySessions,
  revokeSession,
  revokeSessions,
  RevocationScope,
  type DeviceType,
  type Session,
} from '../api/sessionsApi';

/**
 * Devices and sessions.
 *
 * **This screen does not exist in the prototype.** Its design was settled by review on 2026-08-16
 * (`GLOBAL-011`, closing `BLOCK-004`) rather than transcribed, so the reasoning lives in
 * `identity.business.md` §Devices.
 *
 * The shape is a security screen, not a settings screen: the reader is scanning for a device they do
 * not recognise, so each row leads with what identifies it and the destructive action sits at the
 * end of the row rather than behind a menu. The current device is pinned first and cannot be signed
 * out individually — its button would do the same thing as "sign out everywhere", which is offered
 * separately and named plainly.
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

  // This device first, then the rest by most recently seen. A member scanning for something they do
  // not recognise reads from the top, and the one row they can always account for belongs there.
  const live = [...(sessions.data ?? [])].sort((a, b) => {
    if (a.isCurrent !== b.isCurrent) {
      return a.isCurrent ? -1 : 1;
    }

    return new Date(b.lastSeenAt).getTime() - new Date(a.lastSeenAt).getTime();
  });

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
                  secondary={
                    <>
                      {session.approximateLocation ?? session.ipAddress} · last active{' '}
                      {formatRelative(session.lastSeenAt)}
                      <br />
                      {/* The date a device first appeared is what tells a member whether they
                          recognise it. "Last active" alone cannot: an intruder is active now. */}
                      Signed in {formatRelative(session.createdAt)}
                    </>
                  }
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

/**
 * Matches on the name the API sends. This compared against numeric codes until `GLOBAL-011`, which
 * meant nothing ever matched and every row showed the fallback.
 */
export const deviceIconName = (deviceType: DeviceType): string => {
  switch (deviceType) {
    case 'Mobile':
      return 'smartphone';
    case 'Tablet':
      return 'tablet';
    case 'Web':
    case 'Desktop':
      return 'computer';
    default:
      return 'devices';
  }
};

const deviceIcon = (deviceType: DeviceType) => (
  <MaterialSymbol name={deviceIconName(deviceType)} size={20} />
);

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

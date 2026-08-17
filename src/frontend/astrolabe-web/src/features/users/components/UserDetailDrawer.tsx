import {
  Alert,
  Avatar,
  Button,
  Chip,
  Divider,
  Drawer,
  Stack,
  Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { formatDate, money } from '../../membership/planCopy';
import { getUserDetail, type UserAdministrationAction } from '../api/usersApi';
import { USER_STATUS_COLOR, USER_STATUS_ICON, USER_STATUS_LABEL } from '../usersCopy';

/**
 * One account, opened from the directory.
 *
 * A drawer rather than a route: the reader is working through a list and comes straight back to it,
 * and a full page would lose their filter, their sort and their place.
 *
 * The statistics are here for one reason — somebody about to block a member should see the
 * reservations and the fines that will outlive the block before they do it, not afterwards.
 */
export interface UserDetailDrawerProps {
  userId: string | null;
  onClose: () => void;
  onAct: (action: UserAdministrationAction, name: string) => void;
  onResend: (name: string) => void;
}

export const UserDetailDrawer = ({ userId, onClose, onAct, onResend }: UserDetailDrawerProps) => {
  const detail = useQuery({
    queryKey: ['users', 'detail', userId],
    queryFn: () => getUserDetail(userId!),
    enabled: userId !== null,
  });

  const user = detail.data;

  return (
    <Drawer anchor="right" open={userId !== null} onClose={onClose}>
      <Stack sx={{ width: { xs: '100vw', sm: 420 }, p: 3 }} spacing={2.5}>
        {detail.isLoading || !user ? (
          detail.isError ? (
            <ErrorState
              description="We could not load that account."
              onRetry={() => void detail.refetch()}
            />
          ) : (
            <LoadingState label="Loading account…" />
          )
        ) : (
          <>
            <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
              <Avatar sx={{ bgcolor: 'primary.main', width: 48, height: 48 }}>
                {user.fullName
                  .split(' ')
                  .filter(Boolean)
                  .slice(0, 2)
                  .map((part) => part[0]?.toUpperCase() ?? '')
                  .join('')}
              </Avatar>
              <Stack spacing={0.5} sx={{ minWidth: 0 }}>
                <Typography variant="h6" noWrap>
                  {user.fullName}
                </Typography>
                <Chip
                  size="small"
                  color={USER_STATUS_COLOR[user.status]}
                  variant="outlined"
                  icon={<MaterialSymbol name={USER_STATUS_ICON[user.status]} size={16} />}
                  label={USER_STATUS_LABEL[user.status]}
                  sx={{ alignSelf: 'flex-start' }}
                />
              </Stack>
            </Stack>

            <Divider />

            <Stack spacing={1}>
              <Row label="Email" value={user.email} />
              <Row label="Role" value={user.role} />
              {/* Two facts since GLOBAL-019, so two rows. A member's role no longer names a tier. */}
              <Row label="Plan" value={user.plan ?? '—'} />
              <Row label="Home library" value={user.homeLibraryName ?? '—'} />
              <Row label="City" value={user.cityName ?? '—'} />
              <Row label="Member since" value={formatDate(user.createdAt)} />
              <Row
                label="Last activity"
                value={user.lastActiveAt ? formatDate(user.lastActiveAt) : 'Never'}
              />
            </Stack>

            <Divider />

            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
              <Stat label="Active reservations" value={String(user.activeReservations)} />
              <Stat
                label="Outstanding fines"
                value={money(user.outstandingFineCents)}
                warn={user.outstandingFineCents > 0}
              />
              <Stat label="Purchases" value={String(user.purchases)} />
              <Stat
                label="On-time returns"
                // Null is "nothing returned yet". Rendering 0% would read as a terrible record
                // rather than no record.
                value={user.onTimeReturnPercent === null ? '—' : `${user.onTimeReturnPercent}%`}
              />
            </Stack>

            {/* A control that cannot be used has to say why, or the console looks broken. */}
            {user.administrationBlockedReason ? (
              <Alert severity="info" icon={<MaterialSymbol name="lock" size={20} />}>
                {user.administrationBlockedReason}
              </Alert>
            ) : (
              <Stack spacing={1}>
                {user.status === 'Active' ? (
                  <Button
                    color="error"
                    variant="outlined"
                    startIcon={<MaterialSymbol name="block" size={18} />}
                    onClick={() => onAct('Block', user.fullName)}
                  >
                    Block this user
                  </Button>
                ) : null}

                {user.status === 'Blocked' ? (
                  <Button
                    variant="outlined"
                    startIcon={<MaterialSymbol name="lock_open" size={18} />}
                    onClick={() => onAct('Unblock', user.fullName)}
                  >
                    Restore access
                  </Button>
                ) : null}

                {user.status === 'Deleted' ? (
                  <Button
                    variant="outlined"
                    startIcon={<MaterialSymbol name="restore_from_trash" size={18} />}
                    onClick={() => onAct('Restore', user.fullName)}
                  >
                    Restore this account
                  </Button>
                ) : (
                  <Button
                    color="error"
                    startIcon={<MaterialSymbol name="person_off" size={18} />}
                    onClick={() => onAct('Delete', user.fullName)}
                  >
                    Delete this account
                  </Button>
                )}

                {user.status === 'PendingVerification' ? (
                  <Button
                    startIcon={<MaterialSymbol name="forward_to_inbox" size={18} />}
                    onClick={() => onResend(user.fullName)}
                  >
                    Resend verification email
                  </Button>
                ) : null}
              </Stack>
            )}
          </>
        )}
      </Stack>
    </Drawer>
  );
};

const Row = ({ label, value }: { label: string; value: string }) => (
  <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between' }}>
    <Typography variant="body2" color="text.secondary">
      {label}
    </Typography>
    <Typography variant="body2" sx={{ textAlign: 'right', wordBreak: 'break-word' }}>
      {value}
    </Typography>
  </Stack>
);

const Stat = ({ label, value, warn }: { label: string; value: string; warn?: boolean }) => (
  <Stack
    spacing={0.25}
    sx={{
      flex: '1 1 45%',
      p: 1.5,
      borderRadius: '8px',
      border: 1,
      borderColor: warn ? 'error.light' : 'divider',
    }}
  >
    <Typography variant="caption" color="text.secondary">
      {label}
    </Typography>
    <Typography variant="h6" color={warn ? 'error.main' : 'text.primary'}>
      {value}
    </Typography>
  </Stack>
);

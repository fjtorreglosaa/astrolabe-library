import {
  Alert,
  Chip,
  InputAdornment,
  Pagination,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TableSortLabel,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState } from '../../../shared/components/StateViews';
import { TableSkeleton } from '../../../shared/components/TableSkeleton';
import { formatDate } from '../../membership/planCopy';
import { useAuth } from '../../auth/components/AuthProvider';
import {
  administerUser,
  resendVerificationForUser,
  searchUsers,
  type SortDirection,
  type UserAdministrationAction,
  type UserSortKey,
  type UserStatus,
} from '../api/usersApi';
import {
  ACTION_COPY,
  SCOPE_NOTE,
  STATUS_FILTERS,
  USER_STATUS_COLOR,
  USER_STATUS_ICON,
  USER_STATUS_LABEL,
} from '../usersCopy';
import { UserDetailDrawer } from '../components/UserDetailDrawer';

const PAGE_SIZE = 20;

interface Column {
  key: UserSortKey | null;
  label: string;
}

/** Transcribed from the prototype's table: User, Email, Role, Status, Library, Member since. */
const COLUMNS: Column[] = [
  { key: 'FullName', label: 'User' },
  { key: 'Email', label: 'Email' },
  { key: 'Role', label: 'Role' },
  { key: 'Status', label: 'Status' },
  { key: null, label: 'Library' },
  { key: 'CreatedAt', label: 'Member since' },
];

/**
 * The staff user directory.
 *
 * <p>
 * What a caller sees is decided entirely by the server (BR-NET-006, BR-NET-010): an administrator
 * gets the members of the cities their libraries sit in, a super administrator gets the network, and
 * an administrator with no assignments gets nothing. This screen never filters by scope itself —
 * doing so would put one rule in two places, and the client's copy would be the one that drifts.
 * </p>
 * <p>
 * Deleted accounts are hidden until asked for, because this is the one screen a deletion can be
 * undone from and they have to be reachable somewhere.
 * </p>
 */
export const AdminUsersPage = () => {
  const queryClient = useQueryClient();
  const { role } = useAuth();

  const [term, setTerm] = useState('');
  const [status, setStatus] = useState<UserStatus | 'All'>('All');
  const [sortBy, setSortBy] = useState<UserSortKey>('CreatedAt');
  const [direction, setDirection] = useState<SortDirection>('Descending');
  const [page, setPage] = useState(1);
  const [openUserId, setOpenUserId] = useState<string | null>(null);
  const [pending, setPending] = useState<
    { userId: string; name: string; action: UserAdministrationAction } | null
  >(null);
  const [notice, setNotice] = useState<string | null>(null);

  const users = useQuery({
    queryKey: ['users', 'search', term, status, sortBy, direction, page],
    queryFn: () =>
      searchUsers({
        term,
        status: status === 'All' ? undefined : status,
        // Asked for only when the reader asked for them. BR-IDN-008 keeps them out otherwise.
        includeDeleted: status === 'Deleted',
        sortBy,
        direction,
        page,
        pageSize: PAGE_SIZE,
      }),
  });

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: ['users'] });
  };

  const act = useMutation({
    meta: { silent: true },
    mutationFn: () => administerUser(pending!.userId, pending!.action),
    onSuccess: async () => {
      setNotice(`“${pending!.name}” — done.`);
      setPending(null);
      await refresh();
    },
  });

  const resend = useMutation({
    mutationFn: (userId: string) => resendVerificationForUser(userId),
    onSuccess: () => setNotice('Verification email sent again.'),
  });

  const sortOn = (key: UserSortKey) => {
    if (sortBy === key) {
      setDirection(direction === 'Ascending' ? 'Descending' : 'Ascending');
      return;
    }

    setSortBy(key);
    setDirection('Ascending');
  };

  const copy = pending ? ACTION_COPY[pending.action](pending.name) : null;

  return (
    <Stack spacing={3}>
      <Stack spacing={0.5}>
        <Typography variant="h4">Users</Typography>
        <Typography variant="body2" color="text.secondary">
          Everyone you administer, and what you may do about them.
        </Typography>
      </Stack>

      {/* An administrator seeing a short list may think the directory is broken. */}
      {role === 'Admin' ? (
        <Alert severity="info" icon={<MaterialSymbol name="filter_alt" size={20} />}>
          {SCOPE_NOTE}
        </Alert>
      ) : null}

      {notice ? (
        <Alert severity="success" onClose={() => setNotice(null)}>
          {notice}
        </Alert>
      ) : null}

      {act.isError ? (
        <Alert severity="error" onClose={() => act.reset()}>
          {(act.error as { response?: { data?: { title?: string } } })?.response?.data?.title ??
            'We could not complete that action.'}
        </Alert>
      ) : null}

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ alignItems: 'center' }}>
        <TextField
          fullWidth
          size="small"
          placeholder="Search by name or email"
          value={term}
          onChange={(event) => {
            setTerm(event.target.value);
            setPage(1);
          }}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <MaterialSymbol name="search" size={20} />
                </InputAdornment>
              ),
            },
          }}
        />

        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
          {STATUS_FILTERS.map((option) => (
            <Chip
              key={option}
              size="small"
              variant={status === option ? 'filled' : 'outlined'}
              color={status === option ? 'primary' : 'default'}
              label={option === 'All' ? 'All' : USER_STATUS_LABEL[option]}
              onClick={() => {
                setStatus(option);
                setPage(1);
              }}
            />
          ))}
        </Stack>
      </Stack>

      {users.isLoading ? (
        <TableSkeleton rows={5} label="Loading the directory" />
      ) : users.isError || !users.data ? (
        <ErrorState
          description="We could not load the directory."
          onRetry={() => void users.refetch()}
        />
      ) : users.data.items.length === 0 ? (
        <EmptyState
          title="Nobody here"
          description={
            term || status !== 'All'
              ? 'No account matches that filter.'
              : 'No account falls within the libraries you administer.'
          }
        />
      ) : (
        <>
          <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  {COLUMNS.map((column) => (
                    <TableCell key={column.label}>
                      {column.key ? (
                        <TableSortLabel
                          active={sortBy === column.key}
                          direction={direction === 'Ascending' ? 'asc' : 'desc'}
                          onClick={() => sortOn(column.key!)}
                        >
                          {column.label}
                        </TableSortLabel>
                      ) : (
                        column.label
                      )}
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {users.data.items.map((user) => (
                  <TableRow
                    key={user.id}
                    hover
                    sx={{ cursor: 'pointer' }}
                    onClick={() => setOpenUserId(user.id)}
                  >
                    <TableCell>{user.fullName}</TableCell>
                    <TableCell>{user.email}</TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
                        <Typography variant="body2">{user.role}</Typography>
                        {/* The plan is a separate fact from the role since GLOBAL-019. The
                            prototype had one column because the two used to be one field. */}
                        {user.plan ? (
                          <Chip size="small" variant="outlined" label={user.plan} />
                        ) : null}
                      </Stack>
                    </TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        variant="outlined"
                        color={USER_STATUS_COLOR[user.status]}
                        icon={<MaterialSymbol name={USER_STATUS_ICON[user.status]} size={16} />}
                        label={USER_STATUS_LABEL[user.status]}
                      />
                    </TableCell>
                    <TableCell>
                      {user.cityName ? `${user.cityName} — ${user.homeLibraryName ?? '—'}` : '—'}
                    </TableCell>
                    <TableCell>{formatDate(user.createdAt)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Paper>

          {users.data.totalPages > 1 ? (
            <Stack sx={{ alignItems: 'center' }}>
              <Pagination
                count={users.data.totalPages}
                page={users.data.page}
                onChange={(_event, next) => setPage(next)}
                color="primary"
              />
            </Stack>
          ) : null}
        </>
      )}

      <UserDetailDrawer
        userId={openUserId}
        onClose={() => setOpenUserId(null)}
        onAct={(action, name) =>
          setPending({ userId: openUserId!, name, action })
        }
        onResend={() => resend.mutate(openUserId!)}
      />

      <ConfirmDialog
        open={pending !== null}
        title={copy?.title ?? ''}
        description={copy?.body ?? ''}
        confirmLabel={copy?.confirmLabel ?? 'Confirm'}
        destructive={copy?.destructive ?? false}
        busy={act.isPending}
        onConfirm={() => act.mutate()}
        onCancel={() => setPending(null)}
      />
    </Stack>
  );
};

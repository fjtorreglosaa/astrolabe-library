import {
  Alert,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  MenuItem,
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
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/StateViews';
import { formatDate } from '../../membership/planCopy';
import { getCitiesByCountry, getRegistrationCountries } from '../../auth/api/networkApi';
import {
  createLibrary,
  deactivateLibrary,
  getAdmins,
  getLibraries,
  revokeAdmin,
  type Library,
  type LibraryObligations,
} from '../api/networkApi';
import { InviteAdminDialog } from '../components/InviteAdminDialog';
import { WITHDRAW_BODY, WITHDRAW_TITLE, obligationsSummary } from '../networkCopy';

/**
 * Libraries and administrators. Super administrator only (BR-NET-008).
 *
 * <p>
 * Withdrawing a branch is the delicate act here. It is refused only when the branch is its city's
 * home library, because BR-NET-005 offers deactivation as the alternative that preserves history —
 * copies, live loans and unpaid fines are <b>reported</b>, not refused. So the confirmation says
 * what is still outstanding before the click, and the result says what was outstanding after it.
 * That report is the operator's next piece of work and nothing else in the system will chase it.
 * </p>
 */
export const AdminLibrariesPage = () => {
  const queryClient = useQueryClient();

  const [creating, setCreating] = useState(false);
  const [countryId, setCountryId] = useState('');
  const [cityId, setCityId] = useState('');
  const [name, setName] = useState('');
  const [inviting, setInviting] = useState(false);
  const [withdrawing, setWithdrawing] = useState<Library | null>(null);
  const [revoking, setRevoking] = useState<{ id: string; name: string } | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [report, setReport] = useState<LibraryObligations | null>(null);

  const libraries = useQuery({ queryKey: ['network', 'libraries'], queryFn: () => getLibraries() });
  const admins = useQuery({ queryKey: ['network', 'admins'], queryFn: getAdmins });
  const countries = useQuery({
    queryKey: ['network', 'countries'],
    queryFn: getRegistrationCountries,
    enabled: creating,
  });
  const cities = useQuery({
    queryKey: ['network', 'cities', countryId],
    queryFn: () => getCitiesByCountry(countryId),
    enabled: creating && countryId !== '',
  });

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['network'] });

  const create = useMutation({
    mutationFn: () => createLibrary(cityId, name.trim()),
    onSuccess: async () => {
      setNotice(`“${name.trim()}” was added to the network.`);
      setCreating(false);
      setName('');
      setCityId('');
      await refresh();
    },
  });

  const withdraw = useMutation({
    mutationFn: () => deactivateLibrary(withdrawing!.id),
    onSuccess: async (obligations) => {
      setNotice(`“${withdrawing!.name}” was withdrawn. ${obligationsSummary(obligations)}`);
      setReport(obligations.hasAny ? obligations : null);
      setWithdrawing(null);
      await refresh();
    },
  });

  const revoke = useMutation({
    mutationFn: () => revokeAdmin(revoking!.id),
    onSuccess: async () => {
      setNotice(`“${revoking!.name}” no longer administers anything.`);
      setRevoking(null);
      await refresh();
    },
  });

  const failure = create.error ?? withdraw.error ?? revoke.error;

  return (
    <Stack spacing={4}>
      <Stack spacing={0.5}>
        <Typography variant="h4">Libraries &amp; admins</Typography>
        <Typography variant="body2" color="text.secondary">
          The shape of the network, and who runs each part of it.
        </Typography>
      </Stack>

      {notice ? (
        <Alert severity="success" onClose={() => setNotice(null)}>
          {notice}
        </Alert>
      ) : null}

      {report ? (
        <Alert severity="warning" onClose={() => setReport(null)}>
          {/* Reported rather than refused, per BR-NET-005 — and a branch withdrawn with work still
              on it needs a human, because nothing else will chase it. */}
          That branch still holds {report.copies} copies, {report.activeReservations} live
          reservations and {report.unresolvedFines} unresolved fines. Returns and fine payments still
          work for staff; nothing was lost.
        </Alert>
      ) : null}

      {failure ? (
        <Alert severity="error">
          {(failure as { response?: { data?: { title?: string } } })?.response?.data?.title ??
            'We could not complete that.'}
        </Alert>
      ) : null}

      {/* ---------- Libraries ---------- */}
      <Stack spacing={2}>
        <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h6">Libraries</Typography>
          <Button
            variant="contained"
            startIcon={<MaterialSymbol name="add_home_work" size={20} />}
            onClick={() => setCreating(true)}
          >
            Add a library
          </Button>
        </Stack>

        {libraries.isLoading ? (
          <LoadingState label="Loading libraries…" />
        ) : libraries.isError || !libraries.data ? (
          <ErrorState
            description="We could not load the libraries."
            onRetry={() => void libraries.refetch()}
          />
        ) : (
          <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Library</TableCell>
                  <TableCell>State</TableCell>
                  <TableCell align="right" />
                </TableRow>
              </TableHead>
              <TableBody>
                {libraries.data.map((library) => (
                  <TableRow key={library.id} hover>
                    <TableCell>{library.name}</TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={1}>
                        <Chip
                          size="small"
                          variant="outlined"
                          color={library.isActive ? 'success' : 'default'}
                          label={library.isActive ? 'Open' : 'Withdrawn'}
                        />
                        {/* BR-NET-003: a city must always expose one, so this is the one branch
                            that cannot be withdrawn until another is designated. */}
                        {library.isCityHomeLibrary ? (
                          <Chip size="small" variant="outlined" color="primary" label="Home library" />
                        ) : null}
                      </Stack>
                    </TableCell>
                    <TableCell align="right">
                      {library.isActive && !library.isCityHomeLibrary ? (
                        <Button size="small" color="error" onClick={() => setWithdrawing(library)}>
                          Withdraw
                        </Button>
                      ) : null}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Paper>
        )}
      </Stack>

      <Divider />

      {/* ---------- Administrators ---------- */}
      <Stack spacing={2}>
        <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h6">Administrators</Typography>
          <Button
            variant="contained"
            startIcon={<MaterialSymbol name="person_add" size={20} />}
            onClick={() => setInviting(true)}
          >
            Invite an administrator
          </Button>
        </Stack>

        {admins.isLoading ? (
          <LoadingState label="Loading the team…" />
        ) : admins.isError || !admins.data ? (
          <ErrorState description="We could not load the team." onRetry={() => void admins.refetch()} />
        ) : admins.data.length === 0 ? (
          <EmptyState title="No administrators yet" description="Invite one to share the load." />
        ) : (
          <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Email</TableCell>
                  <TableCell>Role</TableCell>
                  <TableCell>Libraries</TableCell>
                  <TableCell>Since</TableCell>
                  <TableCell align="right" />
                </TableRow>
              </TableHead>
              <TableBody>
                {admins.data.map((admin) => (
                  <TableRow key={admin.id} hover>
                    <TableCell>{admin.fullName}</TableCell>
                    <TableCell>{admin.email}</TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={1}>
                        <Chip size="small" variant="outlined" label={admin.role} />
                        {admin.status === 'Invited' ? (
                          <Chip size="small" color="warning" variant="outlined" label="Invited" />
                        ) : null}
                      </Stack>
                    </TableCell>
                    <TableCell>
                      {/* BR-NET-010: an administrator with no assignments can sign in and see
                          nothing, which is a real state and worth showing as one. */}
                      {admin.libraries.length === 0 ? (
                        <Typography variant="caption" color="text.secondary">
                          None assigned
                        </Typography>
                      ) : (
                        admin.libraries.join(' · ')
                      )}
                    </TableCell>
                    <TableCell>{formatDate(admin.since)}</TableCell>
                    <TableCell align="right">
                      {admin.role === 'Admin' ? (
                        <Button
                          size="small"
                          color="error"
                          onClick={() => setRevoking({ id: admin.id, name: admin.fullName })}
                        >
                          Revoke
                        </Button>
                      ) : null}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Paper>
        )}
      </Stack>

      {/* ---------- Dialogs ---------- */}
      <Dialog open={creating} onClose={() => setCreating(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Add a library</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              select
              label="Country"
              value={countryId}
              onChange={(event) => {
                setCountryId(event.target.value);
                setCityId('');
              }}
              fullWidth
            >
              {(countries.data ?? []).map((country) => (
                <MenuItem key={country.id} value={country.id}>
                  {country.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              select
              label="City"
              value={cityId}
              onChange={(event) => setCityId(event.target.value)}
              disabled={countryId === ''}
              fullWidth
            >
              {(cities.data ?? []).map((city) => (
                <MenuItem key={city.id} value={city.id}>
                  {city.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label="Library name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              helperText="Unique within its city (BR-NET-002)."
              fullWidth
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button color="inherit" onClick={() => setCreating(false)}>
            Cancel
          </Button>
          <Button
            variant="contained"
            disabled={!cityId || !name.trim()}
            loading={create.isPending}
            onClick={() => create.mutate()}
          >
            Add
          </Button>
        </DialogActions>
      </Dialog>

      <InviteAdminDialog
        open={inviting}
        libraries={libraries.data ?? []}
        onClose={() => setInviting(false)}
        onInvited={async (message) => {
          setNotice(message);
          setInviting(false);
          await refresh();
        }}
      />

      <ConfirmDialog
        open={withdrawing !== null}
        title={WITHDRAW_TITLE}
        description={WITHDRAW_BODY(withdrawing?.name ?? '')}
        confirmLabel="Withdraw"
        destructive
        busy={withdraw.isPending}
        onConfirm={() => withdraw.mutate()}
        onCancel={() => setWithdrawing(null)}
      />

      <ConfirmDialog
        open={revoking !== null}
        title="Revoke this administrator?"
        description={`“${revoking?.name}” keeps their account and loses every library assignment. It takes effect on their next request.`}
        confirmLabel="Revoke"
        destructive
        busy={revoke.isPending}
        onConfirm={() => revoke.mutate()}
        onCancel={() => setRevoking(null)}
      />
    </Stack>
  );
};

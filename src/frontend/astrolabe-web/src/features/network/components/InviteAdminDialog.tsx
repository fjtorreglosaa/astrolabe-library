import {
  Alert,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation } from '@tanstack/react-query';
import { useState } from 'react';
import type { UserRole } from '../../auth/api/authApi';
import { inviteAdmin, type Library } from '../api/networkApi';

/**
 * Inviting an administrator. Super administrator only (BR-NET-008).
 *
 * <p>
 * The libraries are chosen here and applied on confirmation (BR-NET-014), not granted now — the
 * invitee has no account until they accept. Zero libraries is allowed and is a real state:
 * BR-NET-010 says such an administrator can sign in and see nothing, which is occasionally what you
 * want while somebody is onboarding.
 * </p>
 */
export interface InviteAdminDialogProps {
  open: boolean;
  libraries: Library[];
  onClose: () => void;
  onInvited: (message: string) => void;
}

export const InviteAdminDialog = ({
  open,
  libraries,
  onClose,
  onInvited,
}: InviteAdminDialogProps) => {
  const [email, setEmail] = useState('');
  const [fullName, setFullName] = useState('');
  const [role, setRole] = useState<UserRole>('Admin');
  const [selected, setSelected] = useState<string[]>([]);
  const [message, setMessage] = useState('');

  const close = () => {
    setEmail('');
    setFullName('');
    setRole('Admin');
    setSelected([]);
    setMessage('');
    onClose();
  };

  const invite = useMutation({
    meta: { silent: true },
    mutationFn: () =>
      inviteAdmin({
        email: email.trim(),
        fullName: fullName.trim(),
        role,
        libraryIds: selected,
        message: message.trim() || null,
      }),
    onSuccess: () => {
      onInvited(`Invitation sent to ${email.trim()}.`);
      close();
    },
  });

  const toggle = (libraryId: string) =>
    setSelected((current) =>
      current.includes(libraryId)
        ? current.filter((id) => id !== libraryId)
        : [...current, libraryId],
    );

  return (
    <Dialog open={open} onClose={invite.isPending ? undefined : close} maxWidth="sm" fullWidth>
      <DialogTitle>Invite an administrator</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <TextField
            label="Full name"
            required
            value={fullName}
            onChange={(event) => setFullName(event.target.value)}
            fullWidth
          />
          <TextField
            label="Email"
            required
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            helperText="They choose their own password when they accept — nobody sets it for them."
            fullWidth
          />
          <TextField
            select
            label="Role"
            value={role}
            onChange={(event) => setRole(event.target.value as UserRole)}
            fullWidth
          >
            <MenuItem value="Admin">Admin — full control of assigned libraries</MenuItem>
            <MenuItem value="SuperAdmin">Super Admin — every library, can appoint admins</MenuItem>
          </TextField>

          {role === 'Admin' ? (
            <Stack spacing={0.5}>
              <Typography variant="subtitle2">Libraries</Typography>
              <Typography variant="caption" color="text.secondary">
                Applied when they accept. None is allowed — they will sign in and see nothing until
                you assign some.
              </Typography>
              <Stack sx={{ maxHeight: 220, overflowY: 'auto', mt: 1 }}>
                {libraries
                  .filter((library) => library.isActive)
                  .map((library) => (
                    <FormControlLabel
                      key={library.id}
                      control={
                        <Checkbox
                          size="small"
                          checked={selected.includes(library.id)}
                          onChange={() => toggle(library.id)}
                        />
                      }
                      label={library.name}
                    />
                  ))}
              </Stack>
            </Stack>
          ) : null}

          <TextField
            label="Message"
            value={message}
            onChange={(event) => setMessage(event.target.value)}
            multiline
            minRows={2}
            helperText="Optional. Goes into the invitation email."
            fullWidth
          />

          {invite.isError ? (
            <Alert severity="error">
              {(invite.error as { response?: { data?: { title?: string } } })?.response?.data
                ?.title ?? 'We could not send that invitation.'}
            </Alert>
          ) : null}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button color="inherit" onClick={close} disabled={invite.isPending}>
          Cancel
        </Button>
        <Button
          variant="contained"
          disabled={!email.trim() || !fullName.trim()}
          loading={invite.isPending}
          onClick={() => invite.mutate()}
        >
          Send invitation
        </Button>
      </DialogActions>
    </Dialog>
  );
};

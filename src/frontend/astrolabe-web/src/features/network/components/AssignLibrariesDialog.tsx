import {
  Alert,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Stack,
  Typography,
} from '@mui/material';
import { useMutation } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { assignLibraries, type Admin, type Library } from '../api/networkApi';

/**
 * Changing which libraries an administrator holds.
 *
 * <p>
 * Sends the <b>complete set</b>, not a delta, because that is what the API takes and because a
 * delta would make the screen responsible for knowing what was there before — which is exactly the
 * state that goes stale while a dialog is open.
 * </p>
 * <p>
 * Clearing every box is allowed. BR-NET-010 describes an administrator who can sign in and see
 * nothing as a real state rather than a fault, and it is occasionally what you want while somebody
 * is between posts. The screen says so instead of quietly disabling the button.
 * </p>
 */
export interface AssignLibrariesDialogProps {
  admin: Admin | null;
  libraries: Library[];
  onClose: () => void;
  onAssigned: (message: string) => void;
}

export const AssignLibrariesDialog = ({
  admin,
  libraries,
  onClose,
  onAssigned,
}: AssignLibrariesDialogProps) => {
  const [selected, setSelected] = useState<string[]>([]);

  // The team query returns library *names*, so the current selection is matched by name. Seeded
  // whenever a different administrator is opened, never on every render.
  useEffect(() => {
    if (!admin) {
      return;
    }

    setSelected(
      libraries
        .filter((library) => admin.libraries.includes(library.name))
        .map((library) => library.id),
    );
  }, [admin, libraries]);

  const assign = useMutation({
    meta: { silent: true },
    mutationFn: () => assignLibraries(admin!.id, selected),
    onSuccess: () => {
      onAssigned(
        selected.length === 0
          ? `“${admin!.fullName}” now holds no libraries and will see no administrative data.`
          : `“${admin!.fullName}” now holds ${selected.length} ${
              selected.length === 1 ? 'library' : 'libraries'
            }.`,
      );
      onClose();
    },
  });

  const toggle = (libraryId: string) =>
    setSelected((current) =>
      current.includes(libraryId)
        ? current.filter((id) => id !== libraryId)
        : [...current, libraryId],
    );

  return (
    <Dialog open={admin !== null} onClose={assign.isPending ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Libraries for {admin?.fullName}</DialogTitle>
      <DialogContent>
        <Stack spacing={1} sx={{ pt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            They may act on these branches and no others. Revoking one takes effect on their next
            request — nothing cached survives it.
          </Typography>

          <Stack sx={{ maxHeight: 300, overflowY: 'auto', mt: 1 }}>
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

          {selected.length === 0 ? (
            <Alert severity="info">
              With no libraries they can still sign in, and will see nothing. That is allowed.
            </Alert>
          ) : null}

          {assign.isError ? (
            <Alert severity="error">
              {(assign.error as { response?: { data?: { title?: string } } })?.response?.data
                ?.title ?? 'We could not save that.'}
            </Alert>
          ) : null}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button color="inherit" onClick={onClose} disabled={assign.isPending}>
          Cancel
        </Button>
        <Button variant="contained" loading={assign.isPending} onClick={() => assign.mutate()}>
          Save
        </Button>
      </DialogActions>
    </Dialog>
  );
};

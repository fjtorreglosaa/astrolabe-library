import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import {
  removeBook,
  sendBookToRepair,
  type RemovalReason,
  type RepairReason,
  type StaffBook,
} from '../api/adminCatalogApi';
import { REMOVAL_REASON_LABEL, REPAIR_REASON_LABEL } from '../adminCatalogCopy';

/**
 * Sending a book to repair, or removing it from the collection.
 *
 * <p>
 * Both demand a <b>typed reason</b> rather than free text, and that is the whole point of the
 * dialog. `BR-CAT-025` wants an audit note on every lifecycle change, and a trail is only
 * answerable later if the reasons are a closed set — "why did we lose forty copies last quarter"
 * has an answer when they can be grouped, and none when each one was typed differently.
 * </p>
 * <p>
 * The note is optional and additional. It is where the detail goes, never where the reason goes.
 * </p>
 */
export interface BookLifecycleDialogProps {
  book: StaffBook | null;
  kind: 'repair' | 'remove' | null;
  onClose: () => void;
  onDone: (message: string) => void;
}

export const BookLifecycleDialog = ({
  book,
  kind,
  onClose,
  onDone,
}: BookLifecycleDialogProps) => {
  const queryClient = useQueryClient();
  const [reason, setReason] = useState<string>('');
  const [notes, setNotes] = useState('');

  const isRepair = kind === 'repair';
  const labels = isRepair ? REPAIR_REASON_LABEL : REMOVAL_REASON_LABEL;

  const close = () => {
    setReason('');
    setNotes('');
    onClose();
  };

  const act = useMutation({
    meta: { silent: true },
    mutationFn: async () => {
      if (!book || !kind) {
        return;
      }

      const trimmed = notes.trim() || null;

      if (isRepair) {
        await sendBookToRepair(book.id, reason as RepairReason, trimmed);
        return;
      }

      await removeBook(book.id, reason as RemovalReason, trimmed);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin', 'catalog'] });
      onDone(
        isRepair
          ? `“${book?.title}” is in repair and hidden from members.`
          : `“${book?.title}” was removed from the collection.`,
      );
      close();
    },
  });

  return (
    <Dialog open={book !== null && kind !== null} onClose={act.isPending ? undefined : close} maxWidth="xs" fullWidth>
      <DialogTitle>{isRepair ? 'Send to repair' : 'Remove from the collection'}</DialogTitle>

      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {isRepair
              ? `“${book?.title}” stops being reservable while it is in repair. Reservations already out are unaffected.`
              : `“${book?.title}” leaves the catalogue. Its reservation and purchase history is kept.`}
          </Typography>

          <TextField
            select
            required
            label="Reason"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            // A closed set, so the trail can be grouped and counted later.
            helperText="Recorded in the audit trail."
            fullWidth
          >
            {Object.entries(labels).map(([value, label]) => (
              <MenuItem key={value} value={value}>
                {label}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            label="Note"
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            multiline
            minRows={2}
            helperText="Optional. Detail for whoever reads this later."
            fullWidth
          />

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
          color={isRepair ? 'primary' : 'error'}
          disabled={!reason}
          loading={act.isPending}
          onClick={() => act.mutate()}
        >
          {isRepair ? 'Send to repair' : 'Remove'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

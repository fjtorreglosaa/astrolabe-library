import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from '@mui/material';

/**
 * Confirmation for destructive operations, required by GUIDELINES.md section 39.
 * The prototype always states what will happen, so `description` is mandatory rather than optional.
 */
export interface ConfirmDialogProps {
  open: boolean;
  title: string;
  description: string;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
  busy?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export const ConfirmDialog = ({
  open,
  title,
  description,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  destructive = false,
  busy = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) => (
  <Dialog open={open} onClose={onCancel} aria-labelledby="confirm-title">
    <DialogTitle id="confirm-title">{title}</DialogTitle>
    <DialogContent>
      <DialogContentText>{description}</DialogContentText>
    </DialogContent>
    <DialogActions>
      <Button onClick={onCancel} disabled={busy}>
        {cancelLabel}
      </Button>
      <Button
        onClick={onConfirm}
        disabled={busy}
        variant="contained"
        color={destructive ? 'error' : 'primary'}
        autoFocus
      >
        {busy ? 'Working…' : confirmLabel}
      </Button>
    </DialogActions>
  </Dialog>
);

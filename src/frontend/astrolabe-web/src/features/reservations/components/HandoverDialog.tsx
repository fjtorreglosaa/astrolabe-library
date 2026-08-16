import {
  Alert,
  Button,
  Card,
  CardActionArea,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { beginReturn, type Reservation, type ReturnMethod } from '../api/reservationsApi';
import { RETURN_LABEL, handoverCopy } from '../reservationCopy';

/**
 * The handover: the member gives the copy to a courier or to the desk, and types the code that was
 * read out to them.
 *
 * This does **not** complete the return. The reservation moves to "Return in progress" and stays
 * there until the library checks the copy in — which is the physical truth, and the modal says so
 * rather than implying the loan is over.
 */
export interface HandoverDialogProps {
  reservation: Reservation | null;
  onClose: () => void;
}

export const HandoverDialog = ({ reservation, onClose }: HandoverDialogProps) => {
  const queryClient = useQueryClient();
  const [method, setMethod] = useState<ReturnMethod>('CourierPickup');
  const [code, setCode] = useState('');

  const copy = handoverCopy(method);

  const submit = useMutation({
    mutationFn: () => beginReturn(reservation!.id, method, code),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['reservations'] });
      close();
    },
  });

  const close = () => {
    submit.reset();
    setCode('');
    setMethod('CourierPickup');
    onClose();
  };

  if (!reservation) {
    return null;
  }

  return (
    <Dialog open onClose={submit.isPending ? undefined : close} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack spacing={0.5}>
          <Typography variant="overline" color="primary.main">
            {copy.kicker}
          </Typography>
          <Typography variant="h6">{reservation.title}</Typography>
        </Stack>
      </DialogTitle>

      <DialogContent>
        <Stack spacing={2}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
            {(['CourierPickup', 'LibraryDropOff'] as const).map((option) => (
              <Card
                key={option}
                variant="outlined"
                sx={{
                  flex: 1,
                  borderColor: method === option ? 'primary.main' : 'divider',
                  borderWidth: method === option ? 2 : 1,
                }}
              >
                <CardActionArea
                  sx={{ p: 1.25 }}
                  onClick={() => {
                    setMethod(option);
                    submit.reset();
                  }}
                >
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                    <MaterialSymbol
                      name={method === option ? 'radio_button_checked' : 'radio_button_unchecked'}
                      size={18}
                      sx={{ color: method === option ? 'primary.main' : 'text.secondary' }}
                    />
                    <Typography variant="body2">{RETURN_LABEL[option]}</Typography>
                  </Stack>
                </CardActionArea>
              </Card>
            ))}
          </Stack>

          <Typography variant="body2" color="text.secondary">
            {copy.intro}
          </Typography>

          <TextField
            label={copy.codeLabel}
            placeholder="PU-0000"
            value={code}
            onChange={(event) => {
              setCode(event.target.value);
              submit.reset();
            }}
            fullWidth
            autoFocus
            // The member is copying something said aloud, so the field is forgiving about case and
            // spacing — the server trims and compares case-insensitively.
            slotProps={{ htmlInput: { autoCapitalize: 'characters', spellCheck: false } }}
          />

          {submit.isError ? <Alert severity="error">{copy.rejected}</Alert> : null}
        </Stack>
      </DialogContent>

      <DialogActions>
        <Button onClick={close} color="inherit" disabled={submit.isPending}>
          Cancel
        </Button>
        <Button
          variant="contained"
          disabled={code.trim().length === 0}
          loading={submit.isPending}
          onClick={() => submit.mutate()}
        >
          {copy.confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

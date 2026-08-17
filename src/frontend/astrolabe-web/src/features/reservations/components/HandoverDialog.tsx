import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { useMemberDefaults } from '../../settings/memberDefaults';
import { formatDate } from '../../membership/planCopy';
import { beginReturn, type Reservation, type ReturnMethod } from '../api/reservationsApi';
import { HANDOVER_OUTCOME, RETURN_LABEL, handoverCopy } from '../reservationCopy';

/**
 * The handover: the member gives the copy to a courier or to the desk, and types the code that was
 * read out to them.
 *
 * <p>
 * This does <b>not</b> complete the return. The reservation moves to "Return in progress" and stays
 * there until the library checks the copy in — which is the physical truth, and the modal says so
 * rather than implying the loan is over.
 * </p>
 * <p>
 * The method is taken from the member's saved default, as the prototype does — it reads
 * <c>prefs.ret</c> and never asks. The switch is kept, one quiet line rather than a pair of cards,
 * because the prototype's version strands somebody whose parcel is going back the other way today:
 * their only route to the other method is the settings screen, from a modal they would have to
 * abandon first.
 * </p>
 */
export interface HandoverDialogProps {
  reservation: Reservation | null;
  onClose: () => void;
}

export const HandoverDialog = ({ reservation, onClose }: HandoverDialogProps) => {
  const queryClient = useQueryClient();
  const preferred = useMemberDefaults((state) => state.returns);

  const [method, setMethod] = useState<ReturnMethod>(preferred);
  const [code, setCode] = useState('');

  // Re-seeded when the dialog opens on another reservation, so a one-off switch made last time does
  // not silently become the choice for every return after it.
  useEffect(() => {
    setMethod(preferred);
    setCode('');
  }, [reservation?.id, preferred]);

  const copy = handoverCopy(method);

  const submit = useMutation({
    meta: { success: 'Return started. The library marks it Returned when the copy arrives.', silent: true },
    mutationFn: () => beginReturn(reservation!.id, method, code),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['reservations'] });
      close();
    },
  });

  const close = () => {
    submit.reset();
    setCode('');
    onClose();
  };

  if (!reservation) {
    return null;
  }

  const other: ReturnMethod =
    method === 'CourierPickup' ? 'LibraryDropOff' : 'CourierPickup';

  return (
    <Dialog open onClose={submit.isPending ? undefined : close} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.75, pb: 2 }}>
        {/* The prototype's 42px tinted badge. It is what makes this read as a delivery step rather
            than as another form. */}
        <Box
          aria-hidden
          sx={{
            width: 42,
            height: 42,
            flexShrink: 0,
            borderRadius: '50%',
            bgcolor: 'rgba(14,90,110,.10)',
            display: 'grid',
            placeItems: 'center',
          }}
        >
          <MaterialSymbol
            name={method === 'CourierPickup' ? 'local_shipping' : 'store'}
            size={22}
            sx={{ color: 'primary.main' }}
          />
        </Box>

        <Stack spacing={0.5} sx={{ flex: 1, minWidth: 0 }}>
          <Typography variant="overline" color="text.secondary">
            {copy.kicker}
          </Typography>
          <Typography variant="h5">{reservation.title}</Typography>
          <Typography variant="body2" color="text.secondary">
            {reservation.author} · due {formatDate(reservation.dueOn)}
          </Typography>
        </Stack>

        <IconButton aria-label="Close" onClick={close} disabled={submit.isPending} sx={{ mt: -0.5 }}>
          <MaterialSymbol name="close" size={18} />
        </IconButton>
      </DialogTitle>

      <DialogContent dividers>
        <Stack spacing={2.25}>
          <Typography variant="body2" color="text.secondary">
            {copy.intro}
            {/* Both state names emphasised in place, as the prototype does. They are the two words
                the member will look for on the row afterwards. */}
            <Box component="strong" sx={{ color: 'text.primary' }}>
              {HANDOVER_OUTCOME.before}
            </Box>
            {HANDOVER_OUTCOME.middle}
            <Box component="strong" sx={{ color: 'text.primary' }}>
              {HANDOVER_OUTCOME.after}
            </Box>
            {HANDOVER_OUTCOME.tail}
          </Typography>

          <Stack spacing={1}>
            <Typography variant="overline" color="text.secondary">
              {copy.codeLabel}
            </Typography>
            <TextField
              placeholder="PU-0000"
              value={code}
              onChange={(event) => {
                setCode(event.target.value);
                submit.reset();
              }}
              fullWidth
              autoFocus
              // Sized and tracked like the prototype's field: this is a short code read aloud, and
              // spacing the characters is what makes it checkable against what was said.
              slotProps={{
                htmlInput: {
                  autoCapitalize: 'characters',
                  spellCheck: false,
                  'aria-label': copy.codeLabel,
                  style: { fontSize: 19, letterSpacing: '.14em', fontWeight: 600, height: 52, padding: '0 16px' },
                },
              }}
            />
          </Stack>

          <Button
            size="small"
            color="inherit"
            startIcon={<MaterialSymbol name="swap_horiz" size={18} />}
            onClick={() => {
              setMethod(other);
              submit.reset();
            }}
            sx={{ alignSelf: 'flex-start', textTransform: 'none' }}
          >
            Use {RETURN_LABEL[other].toLowerCase()} instead
          </Button>

          {submit.isError ? <Alert severity="error">{copy.rejected}</Alert> : null}
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button
          variant="contained"
          disabled={code.trim().length === 0}
          loading={submit.isPending}
          onClick={() => submit.mutate()}
          sx={{ flex: 1 }}
        >
          {copy.confirmLabel}
        </Button>
        <Button onClick={close} color="inherit" disabled={submit.isPending}>
          Cancel
        </Button>
      </DialogActions>
    </Dialog>
  );
};

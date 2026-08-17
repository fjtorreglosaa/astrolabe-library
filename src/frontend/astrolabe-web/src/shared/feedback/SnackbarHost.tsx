import { IconButton, Paper, Slide, Snackbar, Stack, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { MaterialSymbol } from '../components/MaterialSymbol';
import { useSnackbarStore } from './snackbarStore';
import { SNACKBAR_DURATION_MS, SNACKBAR_TONES } from './snackbarTones';

/**
 * Shows one queued message at a time, oldest first.
 *
 * <p>
 * Bottom centre, rising into place — the prototype's `snackIn`, which translates by -50% and slides
 * up 24px. Not the corner: this is the one surface in the application that interrupts, and the
 * centre is where the eye already is.
 * </p>
 * <p>
 * One at a time rather than a stack. Three toasts covering the lower third of the screen hide the
 * thing somebody is working on; showing them in turn takes longer and stays legible, which for a
 * message that is already a courtesy is the right trade.
 * </p>
 */
export const SnackbarHost = () => {
  const navigate = useNavigate();
  const queue = useSnackbarStore((state) => state.queue);
  const dismiss = useSnackbarStore((state) => state.dismiss);

  const current = queue[0];

  if (!current) {
    return null;
  }

  const tone = SNACKBAR_TONES[current.tone];
  const actionable = Boolean(current.route);

  return (
    <Snackbar
      key={current.id}
      open
      autoHideDuration={SNACKBAR_DURATION_MS}
      onClose={(_event, reason) => {
        // A click elsewhere is not a dismissal. Closing on `clickaway` makes the message disappear
        // the instant somebody carries on working — which is exactly when they had not read it.
        if (reason !== 'clickaway') {
          dismiss(current.id);
        }
      }}
      anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      slots={{ transition: Slide }}
      slotProps={{ transition: { direction: 'up' } as never }}
    >
      <Paper
        elevation={8}
        role={current.tone === 'error' ? 'alert' : 'status'}
        onClick={
          actionable
            ? () => {
                dismiss(current.id);
                navigate(current.route!);
              }
            : undefined
        }
        sx={{
          display: 'flex',
          alignItems: 'flex-start',
          gap: 1.25,
          px: 2,
          py: 1.5,
          maxWidth: 480,
          borderRadius: '12px',
          bgcolor: tone.background,
          // Fixed against the fixed background above, in both themes.
          color: '#FFFFFF',
          cursor: actionable ? 'pointer' : 'default',
        }}
      >
        <MaterialSymbol name={tone.icon} size={20} sx={{ mt: 0.125, flexShrink: 0 }} />

        <Stack spacing={0.25} sx={{ minWidth: 0 }}>
          <Typography variant="body2" sx={{ fontWeight: 600 }}>
            {current.title}
          </Typography>
          {current.body ? (
            <Typography variant="caption" sx={{ opacity: 0.85 }}>
              {current.body}
            </Typography>
          ) : null}
        </Stack>

        <IconButton
          size="small"
          aria-label="Dismiss"
          onClick={(event) => {
            // Stops the click from also following the route. Dismissing and opening are opposite
            // intentions and the close button must only ever do the first.
            event.stopPropagation();
            dismiss(current.id);
          }}
          sx={{ color: 'inherit', opacity: 0.7, ml: 0.5, mt: -0.25 }}
        >
          <MaterialSymbol name="close" size={16} />
        </IconButton>
      </Paper>
    </Snackbar>
  );
};

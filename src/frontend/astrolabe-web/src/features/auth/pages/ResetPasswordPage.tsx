import { Alert, Box, Button, Paper, Stack, TextField, Typography } from '@mui/material';
import { useMutation } from '@tanstack/react-query';
import { useState } from 'react';
import { Link as RouterLink, useSearchParams } from 'react-router-dom';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { resetPassword } from '../api/authApi';

const MINIMUM_PASSWORD_LENGTH = 12;

/**
 * Setting a new password from the emailed link.
 *
 * <p>
 * Every session ends when the password changes — BR-IDN-013 — and the copy says so before the click
 * rather than after. Somebody recovering an account they think was taken deserves to know that the
 * act they are about to perform is the one that removes whoever took it.
 * </p>
 */
export const ResetPasswordPage = () => {
  const [params] = useSearchParams();
  const token = params.get('token') ?? '';

  const [password, setPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');

  const reset = useMutation({ mutationFn: () => resetPassword(token, password) });

  // Not trimmed, here or on the wire. A password may contain spaces and they are part of it.
  const tooShort = password.length > 0 && password.length < MINIMUM_PASSWORD_LENGTH;
  const mismatched = confirmation.length > 0 && confirmation !== password;
  const ready =
    token !== '' && password.length >= MINIMUM_PASSWORD_LENGTH && confirmation === password;

  return (
    <Paper variant="outlined" sx={{ p: { xs: 3, sm: 4 } }}>
      {reset.isSuccess ? (
        <Stack spacing={2} sx={{ alignItems: 'center', textAlign: 'center' }}>
          <MaterialSymbol name="check_circle" size={44} sx={{ color: 'success.main' }} />
          <Typography variant="h5">Your password is set</Typography>
          <Typography variant="body2" color="text.secondary">
            Every other device has been signed out. Sign in again with the new password.
          </Typography>
          <Button component={RouterLink} to="/login" variant="contained">
            Sign in
          </Button>
        </Stack>
      ) : (
        <Stack spacing={3}>
          <Stack spacing={0.5}>
            <Typography variant="h5">Choose a new password</Typography>
            <Typography variant="body2" color="text.secondary">
              Setting it signs out every device you are signed in on, including any you did not
              recognise.
            </Typography>
          </Stack>

          {token === '' ? (
            <Alert severity="error">
              This link is missing its token. Open the email again, or ask for a new link.
            </Alert>
          ) : null}

          <TextField
            label="New password"
            type="password"
            required
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            error={tooShort}
            helperText={
              tooShort
                ? `At least ${MINIMUM_PASSWORD_LENGTH} characters.`
                : `${MINIMUM_PASSWORD_LENGTH} characters or more. Spaces are allowed and are kept.`
            }
            fullWidth
          />

          <TextField
            label="Repeat the password"
            type="password"
            required
            value={confirmation}
            onChange={(event) => setConfirmation(event.target.value)}
            error={mismatched}
            helperText={mismatched ? 'These do not match.' : ' '}
            fullWidth
          />

          {reset.isError ? (
            <Alert severity="error">
              {(reset.error as { response?: { data?: { title?: string } } })?.response?.data
                ?.title ?? 'That link has expired or has already been used. Ask for a new one.'}
            </Alert>
          ) : null}

          <Box>
            <Button
              variant="contained"
              fullWidth
              disabled={!ready}
              loading={reset.isPending}
              onClick={() => reset.mutate()}
            >
              Set my password
            </Button>
          </Box>
        </Stack>
      )}
    </Paper>
  );
};

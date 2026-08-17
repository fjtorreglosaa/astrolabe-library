import { Alert, Box, Button, Link, Paper, Stack, TextField, Typography } from '@mui/material';
import { useMutation } from '@tanstack/react-query';
import { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { forgotPassword } from '../api/authApi';

/**
 * Asking for a reset link.
 *
 * <p>
 * The confirmation is deliberately the same whether the address has an account or not, and the
 * server behaves the same way. Saying "no account with that address" would turn this form into a way
 * to test which addresses are registered — the same reasoning as BR-IDN-030 on registration.
 * </p>
 */
export const ForgotPasswordPage = () => {
  const [email, setEmail] = useState('');

  const request = useMutation({
    meta: { silent: true }, mutationFn: () => forgotPassword(email.trim()) });

  return (
    <Paper variant="outlined" sx={{ p: { xs: 3, sm: 4 } }}>
      {request.isSuccess ? (
        <Stack spacing={2} sx={{ alignItems: 'center', textAlign: 'center' }}>
          <MaterialSymbol name="mark_email_unread" size={44} sx={{ color: 'primary.main' }} />
          <Typography variant="h5">Check your inbox</Typography>
          <Typography variant="body2" color="text.secondary">
            If an account exists for {email.trim()}, a reset link is on its way. It expires shortly,
            so use it soon.
          </Typography>
          <Button component={RouterLink} to="/login" variant="contained">
            Back to sign in
          </Button>
        </Stack>
      ) : (
        <Stack spacing={3}>
          <Stack spacing={0.5}>
            <Typography variant="h5">Reset your password</Typography>
            <Typography variant="body2" color="text.secondary">
              Tell us the address on the account and we will email you a link.
            </Typography>
          </Stack>

          <TextField
            label="Email address"
            type="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            fullWidth
          />

          {request.isError ? (
            <Alert severity="error">We could not send that just now. Try again shortly.</Alert>
          ) : null}

          <Box>
            <Button
              variant="contained"
              fullWidth
              disabled={!email.trim()}
              loading={request.isPending}
              onClick={() => request.mutate()}
            >
              Send the link
            </Button>
          </Box>

          <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
            Remembered it?{' '}
            <Link component={RouterLink} to="/login">
              Sign in
            </Link>
          </Typography>
        </Stack>
      )}
    </Paper>
  );
};

import {
  Alert,
  Box,
  Button,
  Container,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation } from '@tanstack/react-query';
import { useState } from 'react';
import { Link as RouterLink, useSearchParams } from 'react-router-dom';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { acceptInvitation } from '../api/networkApi';

const MINIMUM_PASSWORD_LENGTH = 12;

/**
 * Accepting a staff invitation.
 *
 * <p>
 * BR-NET-013 says an invited administrator gains no access until they confirm, and until this
 * screen existed there was no way to confirm at all — the command and its anonymous endpoint were
 * built in Stage 1 and never reachable, so every invitation Stage 6 could send was undeliverable.
 * </p>
 * <p>
 * Anonymous by necessity: the invitee has no account to sign into yet, and the token in the link is
 * the only thing that proves they own the address. They choose their own password here; nobody ever
 * sets one for them, which is why an invited account has no password hash until this moment.
 * </p>
 */
export const AcceptInvitationPage = () => {
  const [params] = useSearchParams();
  const token = params.get('token') ?? '';

  const [password, setPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');

  const accept = useMutation({
    mutationFn: () => acceptInvitation(token, password),
  });

  // Not trimmed, here or anywhere. A password may legitimately contain spaces, and quietly removing
  // them would lock somebody out of the account they just created.
  const tooShort = password.length > 0 && password.length < MINIMUM_PASSWORD_LENGTH;
  const mismatched = confirmation.length > 0 && confirmation !== password;
  const ready =
    token !== '' && password.length >= MINIMUM_PASSWORD_LENGTH && confirmation === password;

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper variant="outlined" sx={{ p: 4 }}>
        {accept.isSuccess ? (
          <Stack spacing={2} sx={{ alignItems: 'center', textAlign: 'center' }}>
            <MaterialSymbol name="check_circle" size={48} sx={{ color: 'success.main' }} />
            <Typography variant="h5">Your account is ready</Typography>
            <Typography variant="body2" color="text.secondary">
              You can sign in now. The libraries you were assigned are already yours.
            </Typography>
            <Button component={RouterLink} to="/login" variant="contained">
              Sign in
            </Button>
          </Stack>
        ) : (
          <Stack spacing={3}>
            <Stack spacing={0.5}>
              <Typography variant="h5">Accept your invitation</Typography>
              <Typography variant="body2" color="text.secondary">
                Choose a password and your administrator account is live.
              </Typography>
            </Stack>

            {token === '' ? (
              <Alert severity="error">
                This link is missing its token. Open the invitation email again, or ask the super
                administrator who invited you to send a fresh one.
              </Alert>
            ) : null}

            <TextField
              label="Password"
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

            {accept.isError ? (
              <Alert severity="error">
                {(accept.error as { response?: { data?: { title?: string } } })?.response?.data
                  ?.title ??
                  'We could not accept that invitation. The link may have expired or been replaced.'}
              </Alert>
            ) : null}

            <Box>
              <Button
                variant="contained"
                fullWidth
                disabled={!ready}
                loading={accept.isPending}
                onClick={() => accept.mutate()}
              >
                Accept and create my account
              </Button>
            </Box>

            <Typography variant="caption" color="text.secondary">
              An invitation is single use and expires. If this one has lapsed, ask for another —
              resending replaces every link previously sent to you.
            </Typography>
          </Stack>
        )}
      </Paper>
    </Container>
  );
};

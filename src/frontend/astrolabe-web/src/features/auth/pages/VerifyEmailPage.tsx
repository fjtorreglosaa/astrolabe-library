import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { Alert, Button, Paper, Stack, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { Link as RouterLink, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { toProblemDetails } from '../../../shared/api/httpClient';
import { resendVerification, verifyEmail } from '../api/authApi';

/**
 * Two screens in one, because they are two states of the same step: waiting for the email, and
 * confirming the link once it is opened. Which one shows depends on whether a token is present.
 */
export const VerifyEmailPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const location = useLocation();
  const token = searchParams.get('token');
  const email = (location.state as { email?: string } | null)?.email;

  const [status, setStatus] = useState<'idle' | 'verifying' | 'verified' | 'failed'>('idle');
  const [message, setMessage] = useState<string | null>(null);
  const [resent, setResent] = useState(false);

  useEffect(() => {
    if (!token) {
      return;
    }

    setStatus('verifying');

    verifyEmail(token)
      .then(() => setStatus('verified'))
      .catch((error) => {
        setStatus('failed');
        setMessage(toProblemDetails(error).title ?? 'This link is no longer valid.');
      });
  }, [token]);

  const onResend = async () => {
    if (!email) {
      return;
    }

    // Always reports success: the API cannot confirm whether an address is registered without
    // enabling account enumeration.
    await resendVerification(email);
    setResent(true);
  };

  if (status === 'verified') {
    return (
      <Paper variant="outlined" sx={{ p: 4 }}>
        <Stack spacing={2} sx={{ alignItems: 'center', textAlign: 'center' }}>
          <Typography variant="h5">Your account is active</Typography>
          <Typography variant="body2" color="text.secondary">
            You can sign in and start borrowing.
          </Typography>
          <Button variant="contained" onClick={() => navigate('/login', { replace: true })}>
            Sign in
          </Button>
        </Stack>
      </Paper>
    );
  }

  return (
    <Paper variant="outlined" sx={{ p: 4 }}>
      <Stack spacing={2} sx={{ alignItems: 'center', textAlign: 'center' }}>
        <MaterialSymbol name="mark_email_unread" size={44} sx={{ color: 'primary.main' }} />
        <Typography variant="h5">Check your inbox</Typography>

        {status === 'verifying' ? (
          <Typography variant="body2" color="text.secondary">
            Activating…
          </Typography>
        ) : (
          <Typography variant="body2" color="text.secondary">
            {email
              ? `We sent an activation link to ${email}. Open it to confirm your account and start borrowing.`
              : 'Open the activation link we sent you to confirm your account and start borrowing.'}
          </Typography>
        )}

        {status === 'failed' && message ? <Alert severity="error">{message}</Alert> : null}
        {resent ? <Alert severity="success">If that address has an account, a new link is on its way.</Alert> : null}

        <Stack direction="row" spacing={1}>
          {email ? (
            <Button variant="outlined" onClick={onResend} disabled={resent}>
              {resent ? 'Sent' : 'Resend email'}
            </Button>
          ) : null}
          <Button component={RouterLink} to="/login">
            Back to sign in
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
};

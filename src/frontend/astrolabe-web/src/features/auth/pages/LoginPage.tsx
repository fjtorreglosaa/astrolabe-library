import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  FormControlLabel,
  IconButton,
  Link,
  Paper,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link as RouterLink, useLocation, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { toProblemDetails } from '../../../shared/api/httpClient';
import { rem, typeScale } from '../../../theme/tokens';
import { useAuth } from '../components/AuthProvider';

/**
 * The three seeded accounts, with the shared password from the prototype's own sign-in screen.
 *
 * Clickable rather than merely printed. The prototype lists them as text, and the wording below is
 * unchanged — but typing them by hand invites the browser's password manager to fill a saved
 * credential for a different account, which reads as "this account is broken" rather than as a
 * mistyped password.
 */
const DEMO_ACCOUNTS = [
  { email: 'fjtorreglosaa@gmail.com', role: 'member' },
  { email: 'admin@astrolabe.co', role: 'admin' },
  { email: 'super@astrolabe.co', role: 'super admin' },
] as const;

const DEMO_PASSWORD = 'Testing1234*';

const schema = z.object({
  email: z.string().min(1, 'Enter your email address.').email('Enter a valid email address.'),
  password: z.string().min(1, 'Enter your password.'),
  rememberMe: z.boolean(),
});

type FormValues = z.infer<typeof schema>;

/**
 * Sign-in. Copy is taken from the approved prototype.
 *
 * The form never distinguishes between a wrong password and an unknown, unverified, blocked or
 * locked account: the API returns one message for all of them (BR-IDN-028), and echoing anything
 * more specific would undo that.
 */
export const LoginPage = () => {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [failure, setFailure] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '', rememberMe: true },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFailure(null);

    try {
      await signIn(values.email, values.password);

      const from = (location.state as { from?: string } | null)?.from;
      navigate(from ?? '/home', { replace: true });
    } catch (error) {
      setFailure(toProblemDetails(error).title ?? 'The email address or password is incorrect.');
    }
  });

  return (
    <Paper variant="outlined" sx={{ p: { xs: 3, sm: 4 } }}>
      <Stack spacing={3} component="form" onSubmit={onSubmit} noValidate>
        <Stack spacing={0.5}>
          <Typography variant="h5">Sign in to your account</Typography>
          <Typography variant="body2" color="text.secondary">
            Borrow, buy and get books delivered.
          </Typography>
        </Stack>

        {failure ? <Alert severity="error">{failure}</Alert> : null}

        <TextField
          label="Email address"
          type="email"
          autoComplete="email"
          autoFocus
          fullWidth
          error={Boolean(errors.email)}
          helperText={errors.email?.message}
          {...register('email')}
        />

        <TextField
          label="Password"
          type="password"
          autoComplete="current-password"
          fullWidth
          error={Boolean(errors.password)}
          helperText={errors.password?.message}
          {...register('password')}
        />

        <Stack
          direction="row"
          sx={{ alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap' }}
        >
          <FormControlLabel
            control={<Checkbox defaultChecked {...register('rememberMe')} />}
            label="Remember me"
          />
          <Link component={RouterLink} to="/forgot-password" variant="body2" underline="hover">
            Forgot your password?
          </Link>
        </Stack>

        <Button
          type="submit"
          variant="contained"
          size="large"
          disabled={isSubmitting}
          endIcon={isSubmitting ? undefined : <MaterialSymbol name="arrow_forward" size={20} />}
        >
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </Button>

        <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
          Don&apos;t have an account yet?{' '}
          <Link component={RouterLink} to="/signup" underline="hover">
            Sign up
          </Link>
        </Typography>

        <Box sx={{ pt: 1, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="caption" color="text.secondary" component="p" sx={{ pt: 2, pb: 1 }}>
            Demo accounts — click one to fill both fields:
          </Typography>

          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
            {DEMO_ACCOUNTS.map((account) => (
              <Chip
                key={account.email}
                size="small"
                variant="outlined"
                label={`${account.email} ${account.role}`}
                onClick={() => {
                  // Both fields are set together, and the error is cleared: filling one and leaving
                  // a stale password behind is the exact failure this is here to prevent.
                  setFailure(null);
                  setValue('email', account.email, { shouldValidate: true });
                  setValue('password', DEMO_PASSWORD, { shouldValidate: true });
                }}
              />
            ))}
          </Stack>

          {/*
            The password sits alone in its own element, with nothing around it inside the selectable
            region. It used to read "password Testing1234* for all three" on one line, and selecting
            it copied the word "password" into the field — a wrong password that looks like a broken
            account. The API is right to reject it, so the fix belongs here.
          */}
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', pt: 1.5 }}>
            <Typography variant="caption" color="text.secondary">
              Password for all three:
            </Typography>
            <Box
              component="code"
              sx={{
                px: 0.75,
                py: 0.25,
                borderRadius: 1,
                bgcolor: 'action.hover',
                fontSize: rem(typeScale.micro),
              }}
            >
              {DEMO_PASSWORD}
            </Box>
            <Tooltip title={copied ? 'Copied' : 'Copy password'}>
              <IconButton
                size="small"
                aria-label="Copy the demo password"
                onClick={async () => {
                  await navigator.clipboard.writeText(DEMO_PASSWORD);
                  setCopied(true);
                  setTimeout(() => setCopied(false), 2000);
                }}
              >
                <MaterialSymbol name={copied ? 'check' : 'content_copy'} size={16} />
              </IconButton>
            </Tooltip>
          </Stack>
        </Box>
      </Stack>
    </Paper>
  );
};

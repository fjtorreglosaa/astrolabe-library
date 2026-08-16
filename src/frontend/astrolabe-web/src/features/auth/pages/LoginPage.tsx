import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  FormControlLabel,
  Link,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link as RouterLink, useLocation, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { toProblemDetails } from '../../../shared/api/httpClient';
import { useAuth } from '../components/AuthProvider';

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

  const {
    register,
    handleSubmit,
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
          <Typography variant="caption" color="text.secondary" component="p" sx={{ pt: 2 }}>
            Demo accounts — password <code>Testing1234*</code> for all three:
          </Typography>
          <Typography variant="caption" color="text.secondary" component="p">
            fjtorreglosaa@gmail.com member · admin@astrolabe.co admin · super@astrolabe.co super admin.
          </Typography>
        </Box>
      </Stack>
    </Paper>
  );
};

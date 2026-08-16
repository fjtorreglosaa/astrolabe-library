import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import {
  Alert,
  Button,
  Card,
  CardActionArea,
  Chip,
  Link,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { toProblemDetails } from '../../../shared/api/httpClient';
import { getCitiesByCountry, getRegistrationCountries } from '../api/networkApi';
import { register as registerAccount } from '../api/authApi';

/** Plan codes as the API expects them, with the prototype's copy. */
const PLANS = [
  { code: 0, name: 'Basic', price: '$0', per: '/ month', bullets: ['Borrowing at 1 library of your choice', 'Titles included in the Basic catalog', 'No purchase discounts'] },
  { code: 1, name: 'Plus', price: '$6.99', per: '/ month', bullets: ['Borrowing at every library in your city', 'Full catalog with no restrictions', 'Purchase discounts within your city'] },
  { code: 2, name: 'Max', price: '$12.99', per: '/ month', bullets: ['Borrowing at every library on the platform', 'Purchase discounts in every city', 'Points on every purchase'] },
] as const;

const schema = z.object({
  fullName: z.string().min(1, 'Enter your full name.'),
  email: z.string().min(1, 'Enter your email address.').email('Enter a valid email address.'),
  // Mirrors BR-IDN-009. The API enforces it too; this only saves a round trip.
  password: z.string().min(12, 'Use at least 12 characters.'),
  countryId: z.string().min(1, 'Choose your country.'),
  cityId: z.string().min(1, 'Choose your city.'),
  plan: z.number(),
  acceptedTerms: z.literal(true, { message: 'You must accept the terms to continue.' }),
});

type FormValues = z.infer<typeof schema>;

export const SignUpPage = () => {
  const navigate = useNavigate();
  const [failure, setFailure] = useState<string | null>(null);

  const { control, register, handleSubmit, watch, setValue, formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: { fullName: '', email: '', password: '', countryId: '', cityId: '', plan: 0 },
    });

  const countryId = watch('countryId');
  const selectedPlan = watch('plan');

  const countries = useQuery({ queryKey: ['countries'], queryFn: getRegistrationCountries });

  const cities = useQuery({
    queryKey: ['cities', countryId],
    queryFn: () => getCitiesByCountry(countryId),
    enabled: Boolean(countryId),
  });

  const onSubmit = handleSubmit(async (values) => {
    setFailure(null);

    try {
      await registerAccount(values);
      navigate('/verify', { replace: true, state: { email: values.email } });
    } catch (error) {
      setFailure(toProblemDetails(error).title ?? 'We could not create your account.');
    }
  });

  return (
    <Stack spacing={3}>
      <Paper variant="outlined" sx={{ p: { xs: 3, sm: 4 } }}>
        <Stack spacing={3} component="form" onSubmit={onSubmit} noValidate>
          <Stack spacing={0.5}>
            <Typography variant="h5">Create your account</Typography>
            <Typography variant="body2" color="text.secondary">
              We&apos;ll email you a link to activate it.
            </Typography>
          </Stack>

          {failure ? <Alert severity="error">{failure}</Alert> : null}

          <TextField
            label="Full name"
            fullWidth
            error={Boolean(errors.fullName)}
            helperText={errors.fullName?.message}
            {...register('fullName')}
          />

          <TextField
            label="Email address"
            type="email"
            autoComplete="email"
            fullWidth
            error={Boolean(errors.email)}
            helperText={errors.email?.message}
            {...register('email')}
          />

          <TextField
            label="Password"
            type="password"
            autoComplete="new-password"
            fullWidth
            error={Boolean(errors.password)}
            helperText={errors.password?.message ?? 'At least 12 characters.'}
            {...register('password')}
          />

          <Controller
            control={control}
            name="countryId"
            render={({ field }) => (
              <TextField
                {...field}
                select
                label="Country"
                fullWidth
                disabled={countries.isLoading}
                error={Boolean(errors.countryId)}
                helperText={errors.countryId?.message}
                onChange={(event) => {
                  field.onChange(event);
                  // The previous city belongs to the previous country, so it cannot stand.
                  setValue('cityId', '');
                }}
              >
                {(countries.data ?? []).map((country) => (
                  <MenuItem key={country.id} value={country.id}>
                    {country.name}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />

          <Controller
            control={control}
            name="cityId"
            render={({ field }) => (
              <TextField
                {...field}
                select
                label="City"
                fullWidth
                disabled={!countryId || cities.isLoading}
                error={Boolean(errors.cityId)}
                helperText={errors.cityId?.message ?? 'Determines where your plan lets you borrow.'}
              >
                {(cities.data ?? []).map((city) => (
                  <MenuItem key={city.id} value={city.id}>
                    {city.name}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />

          <Stack spacing={1}>
            <Typography variant="subtitle2">Choose your plan</Typography>
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              {PLANS.map((plan) => (
                <Card
                  key={plan.code}
                  variant="outlined"
                  sx={{
                    flex: 1,
                    borderColor: selectedPlan === plan.code ? 'primary.main' : 'divider',
                    borderWidth: selectedPlan === plan.code ? 2 : 1,
                  }}
                >
                  <CardActionArea sx={{ p: 2 }} onClick={() => setValue('plan', plan.code)}>
                    <Stack spacing={1}>
                      <Stack direction="row" sx={{ alignItems: 'baseline', gap: 1 }}>
                        <Typography variant="h6">{plan.name}</Typography>
                        {selectedPlan === plan.code ? (
                          <Chip size="small" label="Selected" color="primary" />
                        ) : null}
                      </Stack>
                      <Typography variant="h5">
                        {plan.price}
                        <Typography component="span" variant="body2" color="text.secondary">
                          {' '}
                          {plan.per}
                        </Typography>
                      </Typography>
                      {plan.bullets.map((bullet) => (
                        <Stack key={bullet} direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
                          <MaterialSymbol name="check" size={16} sx={{ color: 'success.main', mt: '2px' }} />
                          <Typography variant="body2" color="text.secondary">
                            {bullet}
                          </Typography>
                        </Stack>
                      ))}
                    </Stack>
                  </CardActionArea>
                </Card>
              ))}
            </Stack>
            <Typography variant="caption" color="text.secondary">
              You can switch plans any time from Settings. Max plan points accrue on every purchase.
            </Typography>
          </Stack>

          <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
            <input type="checkbox" id="terms" {...register('acceptedTerms')} />
            <Typography component="label" htmlFor="terms" variant="body2" color="text.secondary">
              I accept the terms of service and the use of my borrowing history for recommendations.
            </Typography>
          </Stack>
          {errors.acceptedTerms ? (
            <Typography variant="caption" color="error">
              {errors.acceptedTerms.message}
            </Typography>
          ) : null}

          <Button
            type="submit"
            variant="contained"
            size="large"
            disabled={isSubmitting}
            endIcon={isSubmitting ? undefined : <MaterialSymbol name="arrow_forward" size={20} />}
          >
            {isSubmitting ? 'Creating account…' : 'Create account'}
          </Button>

          <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
            Already have an account?{' '}
            <Link component={RouterLink} to="/login" underline="hover">
              Sign in
            </Link>
          </Typography>
        </Stack>
      </Paper>
    </Stack>
  );
};

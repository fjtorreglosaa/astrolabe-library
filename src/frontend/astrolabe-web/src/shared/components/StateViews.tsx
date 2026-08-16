import { Alert, AlertTitle, Box, Button, CircularProgress, Stack, Typography } from '@mui/material';
import type { ReactNode } from 'react';

/**
 * The three states every data-driven screen must handle explicitly, per GUIDELINES.md section 39.
 * Centralised so loading, empty and error never get improvised per screen.
 */

export const LoadingState = ({ label = 'Loading…' }: { label?: string }) => (
  <Stack spacing={2} sx={{ py: 8, alignItems: 'center', justifyContent: 'center' }} role="status">
    <CircularProgress aria-label={label} />
    <Typography variant="body2" color="text.secondary">
      {label}
    </Typography>
  </Stack>
);

export const EmptyState = ({
  title,
  description,
  action,
}: {
  title: string;
  description?: string;
  action?: ReactNode;
}) => (
  <Stack spacing={1.5} sx={{ py: 8, textAlign: 'center', alignItems: 'center' }}>
    <Typography variant="h6">{title}</Typography>
    {description ? (
      <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 420 }}>
        {description}
      </Typography>
    ) : null}
    {action ? <Box sx={{ pt: 1 }}>{action}</Box> : null}
  </Stack>
);

export const ErrorState = ({
  title = 'Something went wrong.',
  description,
  onRetry,
}: {
  title?: string;
  description?: string;
  onRetry?: () => void;
}) => (
  <Alert
    severity="error"
    sx={{ my: 2 }}
    action={
      onRetry ? (
        <Button color="inherit" size="small" onClick={onRetry}>
          Retry
        </Button>
      ) : undefined
    }
  >
    <AlertTitle>{title}</AlertTitle>
    {description}
  </Alert>
);

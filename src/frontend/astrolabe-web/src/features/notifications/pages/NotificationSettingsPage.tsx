import { Alert, Paper, Stack, Switch, Typography } from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { ErrorState, LoadingState } from '../../../shared/components/StateViews';
import {
  getMyNotifications,
  setNotificationPreference,
  type NotificationFamily,
} from '../api/notificationsApi';
import { FAMILY_LABEL, FAMILY_NOTE } from '../notificationsCopy';

const FAMILIES: NotificationFamily[] = ['Due', 'Payments', 'Returns', 'Holds', 'Support'];

/**
 * Notification settings.
 *
 * <p>
 * Five switches for eight kinds, which is `BR-NTF-002` as the member experiences it: somebody who
 * turns off payments means receipts, desk codes and settled fines alike, and offering three switches
 * where one decision exists is offering them work.
 * </p>
 * <p>
 * A switch off means nothing is created, not that something is hidden — so the copy says "stop
 * sending" rather than "hide", because those are different promises and only one of them is true.
 * </p>
 */
export const NotificationSettingsPage = () => {
  const queryClient = useQueryClient();

  const feed = useQuery({ queryKey: ['notifications'], queryFn: () => getMyNotifications(1) });

  const update = useMutation({
    mutationFn: ({ family, muted }: { family: NotificationFamily; muted: boolean }) =>
      setNotificationPreference(family, muted),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  });

  const muted = feed.data?.mutedFamilies ?? [];

  return (
    <Stack spacing={3}>
      <Stack spacing={0.5}>
        <Typography variant="h4">Notification settings</Typography>
        <Typography variant="body2" color="text.secondary">
          Choose what reaches you. Turning a family off stops it being sent at all — nothing is
          collected and hidden.
        </Typography>
      </Stack>

      {muted.length === FAMILIES.length ? (
        <Alert severity="info" icon={<MaterialSymbol name="notifications_off" size={20} />}>
          Everything is off. Fines still accrue and returns still arrive — you simply will not be
          told about them here.
        </Alert>
      ) : null}

      {feed.isLoading ? (
        <LoadingState label="Loading your settings…" />
      ) : feed.isError ? (
        <ErrorState
          description="We could not load your settings."
          onRetry={() => void feed.refetch()}
        />
      ) : (
        <Paper variant="outlined">
          <Stack divider={<div />}>
            {FAMILIES.map((family) => (
              <Stack
                key={family}
                direction="row"
                spacing={2}
                sx={{
                  p: 2,
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  borderBottom: 1,
                  borderColor: 'divider',
                  '&:last-of-type': { borderBottom: 0 },
                }}
              >
                <Stack spacing={0.25}>
                  <Typography variant="subtitle2">{FAMILY_LABEL[family]}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {FAMILY_NOTE[family]}
                  </Typography>
                </Stack>
                <Switch
                  // Checked means "on", so the stored value — which records a mute — is inverted
                  // once, here, at the edge.
                  checked={!muted.includes(family)}
                  disabled={update.isPending}
                  onChange={(event) =>
                    update.mutate({ family, muted: !event.target.checked })
                  }
                  slotProps={{ input: { 'aria-label': FAMILY_LABEL[family] } }}
                />
              </Stack>
            ))}
          </Stack>
        </Paper>
      )}
    </Stack>
  );
};

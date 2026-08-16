import { Alert, Button, Paper, Stack, Typography } from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { ErrorState, LoadingState } from '../../../shared/components/StateViews';
import {
  cancelScheduledPlanChange,
  changePlan,
  getMyMembership,
  getPlans,
  quotePlanChange,
  type PlanOption,
  type PlanTier,
} from '../api/membershipApi';
import { PlanCard } from '../components/PlanCard';
import { PlanChangeDialog } from '../components/PlanChangeDialog';
import { REACH_LABEL, formatDate, pendingChangeLine, planStatusLine } from '../planCopy';

/**
 * Membership.
 *
 * Transcribed from the prototype's Settings › Membership section: the status card, the pending
 * change strip, and the three plans side by side. Changing a plan opens the two-step dialog rather
 * than acting on the click, because BR-MBR-020 requires the cost and the losses to be shown first.
 */
export const MembershipPage = () => {
  const queryClient = useQueryClient();
  const [target, setTarget] = useState<PlanTier | null>(null);

  const membership = useQuery({ queryKey: ['membership'], queryFn: getMyMembership });
  const plans = useQuery({ queryKey: ['membership', 'plans'], queryFn: getPlans });

  const quote = useQuery({
    queryKey: ['membership', 'quote', target],
    queryFn: () => quotePlanChange(target!),
    enabled: target !== null,
  });

  // Both mutations invalidate the same keys: a plan change moves the status card, the pending strip
  // and every badge in the comparison at once, so refetching one without the others shows a screen
  // that disagrees with itself.
  const refreshMembership = async () => {
    await queryClient.invalidateQueries({ queryKey: ['membership'] });
  };

  const change = useMutation({
    mutationFn: changePlan,
    onSuccess: async () => {
      setTarget(null);
      await refreshMembership();
    },
  });

  const cancelChange = useMutation({
    mutationFn: cancelScheduledPlanChange,
    onSuccess: refreshMembership,
  });

  if (membership.isLoading || plans.isLoading) {
    return <LoadingState label="Loading your membership…" />;
  }

  if (membership.isError || plans.isError || !membership.data || !plans.data) {
    return (
      <ErrorState
        description="We could not load your membership."
        onRetry={() => {
          void membership.refetch();
          void plans.refetch();
        }}
      />
    );
  }

  const current = membership.data;
  const pendingLine = pendingChangeLine(current);

  const openChange = (option: PlanOption) => {
    change.reset();
    setTarget(option.plan);
  };

  return (
    <Stack spacing={3}>
      <Stack spacing={0.5}>
        <Typography variant="h4">Membership</Typography>
        <Typography variant="body2" color="text.secondary">
          Your plan sets where you can borrow and how discounts apply.
        </Typography>
      </Stack>

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
            <MaterialSymbol name="workspace_premium" size={28} sx={{ color: 'primary.main' }} />
            <Stack spacing={0.25}>
              <Typography variant="h6">{current.plan}</Typography>
              <Typography variant="body2" color="text.secondary">
                {planStatusLine(current)}
              </Typography>
            </Stack>
          </Stack>

          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={{ xs: 1, sm: 4 }}
            sx={{ flexWrap: 'wrap' }}
          >
            <Detail label="Borrowing" value={REACH_LABEL[current.reach]} />
            <Detail label="Home library" value={current.homeLibraryName ?? 'Not set'} />
            <Detail label="City" value={current.cityName ?? 'Not set'} />
            <Detail
              label="Renews"
              value={current.priceCents === 0 ? 'No renewal' : formatDate(current.renewsOn)}
            />
          </Stack>
        </Stack>
      </Paper>

      {pendingLine ? (
        <Alert
          severity="info"
          icon={<MaterialSymbol name="schedule" size={20} />}
          action={
            <Button
              size="small"
              color="inherit"
              loading={cancelChange.isPending}
              onClick={() => cancelChange.mutate()}
            >
              Cancel scheduled change
            </Button>
          }
        >
          {pendingLine}
        </Alert>
      ) : null}

      {change.isError ? (
        <Alert severity="error">We could not change your plan. Nothing was charged.</Alert>
      ) : null}

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ alignItems: 'stretch' }}>
        {plans.data.map((option) => (
          <PlanCard key={option.plan} option={option} onSelect={openChange} />
        ))}
      </Stack>

      <PlanChangeDialog
        open={target !== null}
        target={target}
        membership={current}
        quote={quote.data}
        isLoadingQuote={quote.isLoading}
        quoteFailed={quote.isError}
        isSubmitting={change.isPending}
        onRetryQuote={() => void quote.refetch()}
        onConfirm={(plan) => change.mutate(plan)}
        onClose={() => setTarget(null)}
      />
    </Stack>
  );
};

const Detail = ({ label, value }: { label: string; value: string }) => (
  <Stack spacing={0.25}>
    <Typography variant="overline" color="text.secondary">
      {label}
    </Typography>
    <Typography variant="body2">{value}</Typography>
  </Stack>
);

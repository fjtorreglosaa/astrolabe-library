import { Button, Paper, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { getMyMembership } from '../../membership/api/membershipApi';
import { pendingChangeLine, planStatusLine } from '../../membership/planCopy';

/**
 * The plan, as Settings shows it: the state and the scheduled change, and a way through to the
 * screen that can act on either.
 *
 * <p>
 * Read-only on purpose. Changing or cancelling a plan is the membership screen's job — it owns the
 * quote, the loss warnings and the confirmations — and a second place that could start a plan change
 * would be a second place for those rules to drift out of step.
 * </p>
 */
export const MembershipSummaryCard = () => {
  const navigate = useNavigate();
  const membership = useQuery({ queryKey: ['membership'], queryFn: getMyMembership });

  const pending = membership.data ? pendingChangeLine(membership.data) : null;

  return (
    <Stack spacing={2}>
      <Typography variant="h6">Membership</Typography>
      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
          <MaterialSymbol
            name="workspace_premium"
            size={22}
            sx={{ color: 'primary.main', mt: 0.25 }}
          />
          <Stack spacing={0.5} sx={{ flex: 1, minWidth: 0 }}>
            <Typography variant="subtitle2">
              {membership.data ? membership.data.plan : 'Loading…'}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {membership.data ? planStatusLine(membership.data) : ' '}
            </Typography>
            {pending ? (
              <Stack direction="row" spacing={0.75} sx={{ alignItems: 'flex-start', pt: 0.5 }}>
                <MaterialSymbol
                  name="schedule"
                  size={16}
                  sx={{ color: 'warning.main', mt: 0.25 }}
                />
                <Typography variant="caption" color="text.secondary">
                  {pending}
                </Typography>
              </Stack>
            ) : null}
          </Stack>
          <Button size="small" onClick={() => navigate('/settings/membership')}>
            {pending ? 'Manage →' : 'See plans →'}
          </Button>
        </Stack>
      </Paper>
    </Stack>
  );
};

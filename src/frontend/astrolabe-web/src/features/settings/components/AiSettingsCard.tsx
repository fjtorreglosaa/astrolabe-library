import { Button, Paper, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { useAuth } from '../../auth/components/AuthProvider';
import { getLibraryAiStatus } from '../../recommendations/api/recommendationsApi';

/**
 * The AI recommendations entry in Settings.
 *
 * <p>
 * Two different things behind one heading, as in the prototype. Staff get the count of their
 * libraries that are actually connected and a way in; members get told, in the prototype's own
 * words, that there is nothing here for them to set up — with a lock rather than a disabled form,
 * because a form nobody may submit reads as a fault.
 * </p>
 * <p>
 * The count comes from the admin endpoint, so it is fetched for staff only. Showing a member how
 * many libraries are connected would need an endpoint that does not exist and that a member has no
 * authority to call; the sentence they see is true without it.
 * </p>
 */
export const AiSettingsCard = () => {
  const navigate = useNavigate();
  const { user, plan } = useAuth();
  const isStaff = user?.isStaff ?? false;

  const status = useQuery({
    queryKey: ['recommendations', 'library-status'],
    queryFn: getLibraryAiStatus,
    enabled: isStaff,
  });

  const connected = status.data?.filter((library) => library.isConnected).length ?? 0;

  if (isStaff) {
    return (
      <Stack spacing={2}>
        <Stack spacing={0.25}>
          <Typography variant="h6">AI recommendations per library</Typography>
          <Typography variant="body2" color="text.secondary">
            Each library runs on its own key. Members of a connected library get model-generated
            picks; everywhere else they see the most-borrowed fallback.
          </Typography>
        </Stack>
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
            <MaterialSymbol name="auto_awesome" size={22} sx={{ color: 'primary.main' }} />
            <Stack spacing={0.25} sx={{ flex: 1, minWidth: 0 }}>
              <Typography variant="body2">
                {status.isLoading
                  ? 'Checking your libraries…'
                  : `${connected} of ${status.data?.length ?? 0} of your libraries are connected.`}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Keys are stored encrypted and are never shown again once saved.
              </Typography>
            </Stack>
            <Button size="small" onClick={() => navigate('/admin/ai')}>
              Configure →
            </Button>
          </Stack>
        </Paper>
      </Stack>
    );
  }

  // Basic never sees the recommendations surface at all, so the card sells the plan rather than
  // pretending there is a setting behind it.
  const locked = plan === 'Basic' || plan === null;

  return (
    <Stack spacing={2}>
      <Typography variant="h6">AI recommendations</Typography>
      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
          <MaterialSymbol
            name={locked ? 'lock' : 'auto_awesome'}
            size={22}
            sx={{ color: locked ? 'text.disabled' : 'primary.main' }}
          />
          <Typography variant="body2" sx={{ flex: 1, minWidth: 0 }}>
            {locked
              ? 'Model-generated picks come with Plus and Max. On Basic you still get the most-borrowed list.'
              : "Keys are managed by each library's staff, not by members — nothing to set up here."}
          </Typography>
          <Button
            size="small"
            onClick={() => navigate(locked ? '/settings/membership' : '/ai')}
          >
            {locked ? 'Compare plans →' : 'See details →'}
          </Button>
        </Stack>
      </Paper>
    </Stack>
  );
};

import { Box, Button, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { MaterialSymbol } from '../shared/components/MaterialSymbol';
import { useAuth } from '../features/auth/components/AuthProvider';
import { getLibraryAiStatus, getMyRecommendations } from '../features/recommendations/api/recommendationsApi';

/**
 * The AI panel at the foot of the sidebar.
 *
 * <p>
 * It is how a Basic member finds out the feature exists. The navigation entry is hidden from them by
 * `requiresPaidPlan`, so without this the surface is invisible rather than closed — and a benefit
 * nobody knows about is a benefit nobody upgrades for. The prototype draws a lock and points at the
 * plans; this does the same.
 * </p>
 * <p>
 * For staff it reports how many of their libraries are connected, which is the number they act on.
 * </p>
 */
export const SidebarAiCard = ({ collapsed }: { collapsed: boolean }) => {
  const navigate = useNavigate();
  const { role, plan } = useAuth();

  const isStaff = role === 'Admin' || role === 'SuperAdmin';
  const hasPlan = plan === 'Plus' || plan === 'Max';

  const libraries = useQuery({
    queryKey: ['recommendations', 'libraries'],
    queryFn: getLibraryAiStatus,
    enabled: isStaff,
  });

  // Asked only when the member may actually have an answer, so a Basic account never triggers a
  // request that exists to be refused.
  const recommendations = useQuery({
    queryKey: ['recommendations'],
    queryFn: getMyRecommendations,
    enabled: !isStaff && hasPlan,
  });

  const connected = (libraries.data ?? []).filter((library) => library.isConnected).length;

  const status = isStaff
    ? connected > 0
      ? `${connected} of ${libraries.data?.length ?? 0} of your libraries connected`
      : 'No library connected yet'
    : !hasPlan
      ? 'Included from the Plus plan'
      : recommendations.data?.source === 'Model'
        ? 'Your library is connected'
        : 'Your library has not enabled it';

  const cta = isStaff ? 'Configure' : hasPlan ? 'See details' : 'Compare plans';
  const target = isStaff ? '/admin/ai' : hasPlan ? '/ai' : '/settings/membership';

  // In the rail there is no room for a sentence, so it becomes the icon it already is — still
  // present, still a way in, rather than vanishing when the sidebar narrows.
  if (collapsed) {
    return (
      <Box sx={{ p: 1, display: 'grid', placeItems: 'center' }}>
        <Button
          onClick={() => navigate(target)}
          aria-label="AI recommendations"
          sx={{ minWidth: 0, p: 1 }}
        >
          <MaterialSymbol name={!isStaff && !hasPlan ? 'lock' : 'auto_awesome'} size={20} />
        </Button>
      </Box>
    );
  }

  return (
    <Box
      sx={{
        m: 1.5,
        p: 1.5,
        borderRadius: 2,
        border: 1,
        borderColor: 'divider',
        bgcolor: 'action.hover',
      }}
    >
      <Stack spacing={0.75}>
        <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
          <MaterialSymbol
            name={!isStaff && !hasPlan ? 'lock' : 'auto_awesome'}
            size={18}
            sx={{ color: 'primary.main' }}
          />
          <Typography variant="subtitle2">AI recommendations</Typography>
        </Stack>

        <Typography variant="caption" color="text.secondary">
          {status}
        </Typography>

        <Button
          size="small"
          endIcon={<MaterialSymbol name="arrow_forward" size={16} />}
          onClick={() => navigate(target)}
          sx={{ alignSelf: 'flex-start', px: 0 }}
        >
          {cta}
        </Button>
      </Stack>
    </Box>
  );
};

import { Box, Button, Paper, Skeleton, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { useAuth } from '../../auth/components/AuthProvider';
import { tintFor } from '../../catalog/catalogCopy';
import { getMyRecommendations } from '../../recommendations/api/recommendationsApi';

/** How many the prototype shows on the dashboard. The full list has its own screen. */
const SHOWN = 3;

/**
 * The dashboard's recommendations panel — the prototype's "Picked for you".
 *
 * <p>
 * <b>A dark card in a light page, on purpose.</b> The prototype paints it with its own gradient
 * rather than the surface colour used by everything around it, which is what makes the one
 * suggested thing on the screen look unlike the six reported things. Reproduced literally, in both
 * themes, because the contrast is against the page rather than against the theme.
 * </p>
 * <p>
 * Basic gets a different card, not a disabled one: a dashed outline, a lock, and the plans. A greyed
 * panel says "broken"; this says "not yours yet", which is the truth.
 * </p>
 */
export const HomeRecommendationsCard = () => {
  const navigate = useNavigate();
  const { plan } = useAuth();

  const locked = plan === 'Basic' || plan === null;

  const recommendations = useQuery({
    queryKey: ['recommendations', 'mine'],
    queryFn: getMyRecommendations,
    enabled: !locked,
  });

  if (locked) {
    return (
      <Paper
        variant="outlined"
        sx={{ p: 2.75, borderStyle: 'dashed', borderRadius: '12px' }}
      >
        <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
          <Box
            aria-hidden
            sx={{
              width: 34,
              height: 34,
              borderRadius: '50%',
              bgcolor: 'action.hover',
              display: 'grid',
              placeItems: 'center',
            }}
          >
            <MaterialSymbol name="lock" size={19} sx={{ color: 'text.secondary' }} />
          </Box>
          <Typography variant="h6">Recommendations</Typography>
        </Stack>

        <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5, lineHeight: 1.6 }}>
          Personalised picks come with the Plus and Max plans. On Basic you can still browse the
          catalog and reserve at your home library.
        </Typography>

        <Button
          variant="contained"
          fullWidth
          sx={{ mt: 2.25 }}
          onClick={() => navigate('/settings/membership')}
        >
          Compare plans
        </Button>
      </Paper>
    );
  }

  const items = recommendations.data?.items.slice(0, SHOWN) ?? [];

  return (
    <Paper
      elevation={0}
      sx={{
        p: 2.5,
        borderRadius: '12px',
        // The prototype's own gradient and text colours, fixed in both themes.
        background: 'linear-gradient(150deg,#0B2E3B,#06202A)',
        color: '#E6F5F3',
      }}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <MaterialSymbol name="auto_awesome" size={20} />
        <Typography variant="overline">Picked for you</Typography>
      </Stack>

      <Stack spacing={1.75} sx={{ mt: 1.75 }}>
        {recommendations.isLoading
          ? Array.from({ length: SHOWN }, (_, index) => (
              <Stack key={index} direction="row" spacing={1.5}>
                <Skeleton
                  variant="rounded"
                  width={38}
                  height={54}
                  sx={{ bgcolor: 'rgba(255,255,255,.10)', flexShrink: 0 }}
                />
                <Stack spacing={0.875} sx={{ flex: 1, minWidth: 0 }}>
                  <Skeleton height={15} width="70%" sx={{ bgcolor: 'rgba(255,255,255,.14)' }} />
                  <Skeleton height={11} width="45%" sx={{ bgcolor: 'rgba(255,255,255,.09)' }} />
                  <Skeleton height={11} width="90%" sx={{ bgcolor: 'rgba(255,255,255,.07)' }} />
                </Stack>
              </Stack>
            ))
          : items.map((item) => (
              <Stack key={item.bookId} direction="row" spacing={1.5}>
                <Box
                  aria-hidden
                  sx={{
                    width: 38,
                    height: 54,
                    flexShrink: 0,
                    borderRadius: '3px',
                    bgcolor: tintFor(item.bookId),
                    backgroundImage: item.coverUrl ? `url("${item.coverUrl}")` : undefined,
                    backgroundSize: 'cover',
                    backgroundPosition: 'center',
                  }}
                />
                <Box sx={{ minWidth: 0 }}>
                  <Typography variant="h6" sx={{ fontSize: '1rem' }}>
                    {item.title}
                  </Typography>
                  <Typography variant="caption" sx={{ display: 'block', color: '#93C7C0', mt: 0.25 }}>
                    {item.author}
                  </Typography>
                  {/* The reason, not the match percentage. A sentence a member can disagree with is
                      worth more than a number they cannot check. */}
                  <Typography variant="caption" sx={{ display: 'block', color: '#C4E4DF', mt: 0.75, lineHeight: 1.5 }}>
                    {item.reason}
                  </Typography>
                </Box>
              </Stack>
            ))}

        {!recommendations.isLoading && items.length === 0 ? (
          <Typography variant="body2" sx={{ color: '#C4E4DF' }}>
            Nothing yet. Borrow and return a couple of books and picks start appearing here.
          </Typography>
        ) : null}
      </Stack>

      <Button
        fullWidth
        variant="outlined"
        onClick={() => navigate('/ai')}
        sx={{
          mt: 2.25,
          color: '#fff',
          borderColor: 'rgba(255,255,255,.35)',
          '&:hover': { borderColor: '#fff', bgcolor: 'rgba(255,255,255,.12)' },
        }}
      >
        See all recommendations
      </Button>
    </Paper>
  );
};

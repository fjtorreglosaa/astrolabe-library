import { Chip, Tooltip } from '@mui/material';
import { MaterialSymbol } from '../shared/components/MaterialSymbol';
import { useRealtimeStatus } from '../shared/realtime/RealtimeProvider';

/**
 * Says so when the screen has stopped updating on its own.
 *
 * <p>
 * <b>Silent while it is working.</b> A green "live" badge is a permanent claim that has to be true
 * every second, and the moment it is briefly wrong it has taught the reader to ignore it. Screens
 * updating by themselves is the expected state and needs no announcement; the thing worth an
 * interruption is the loss of it.
 * </p>
 * <p>
 * The wording says what it means for the reader — "may be out of date" — rather than naming a
 * transport. Nobody outside this codebase knows what a WebSocket is, and "disconnected" invites the
 * reasonable but wrong conclusion that the app has stopped working. It has not: every screen still
 * loads and every action still goes through, they simply wait for a refresh.
 * </p>
 */
export const RealtimeIndicator = () => {
  const status = useRealtimeStatus();

  if (status === 'live' || status === 'connecting') {
    return null;
  }

  const reconnecting = status === 'reconnecting';

  return (
    <Tooltip
      title={
        reconnecting
          ? 'Reconnecting. Anything you do still works — this page just will not update on its own until it is back.'
          : 'Not receiving live updates. Everything still works; reload to see the latest.'
      }
    >
      <Chip
        size="small"
        variant="outlined"
        color={reconnecting ? 'warning' : 'default'}
        icon={<MaterialSymbol name={reconnecting ? 'sync' : 'cloud_off'} size={16} />}
        label={reconnecting ? 'Reconnecting' : 'May be out of date'}
        sx={{
          // Never a control. It reports; there is nothing here to click.
          cursor: 'default',
          '& .MuiChip-icon': reconnecting
            ? { animation: 'astrolabe-spin 1.4s linear infinite' }
            : undefined,
          '@keyframes astrolabe-spin': { to: { transform: 'rotate(360deg)' } },
          // Respects a reader who asked the system for less motion.
          '@media (prefers-reduced-motion: reduce)': {
            '& .MuiChip-icon': { animation: 'none' },
          },
        }}
      />
    </Tooltip>
  );
};

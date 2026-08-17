import {
  Badge,
  Button,
  Divider,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Popover,
  Stack,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAnnounceNewNotifications } from './useAnnounceNewNotifications';
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import {
  clearNotifications,
  getMyNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  type Notification,
} from '../api/notificationsApi';
import {
  ALL_MUTED_BODY,
  ALL_MUTED_TITLE,
  CLEAR_CONFIRM_BODY,
  CLEAR_CONFIRM_TITLE,
  EMPTY_BODY,
  EMPTY_TITLE,
  KIND_COLOR,
  KIND_ICON,
} from '../notificationsCopy';

const ALL_FAMILIES = 5;

/**
 * The bell, its badge and the list behind it.
 *
 * <p>
 * The badge is the server's count, not a length. BR-NTF-010 counts every unread notification while
 * this list holds at most thirty — deriving the badge from `items.length` would make it stop growing
 * at thirty, which is exactly the point at which a member most needs it to keep going.
 * </p>
 * <p>
 * "Notifications are off" and "nothing new yet" are different empty states on purpose. One is a
 * choice the member made and can undo; the other is good news.
 * </p>
 */
export const NotificationBell = () => {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const [confirmingClear, setConfirmingClear] = useState(false);

  const feed = useQuery({
    queryKey: ['notifications'],
    queryFn: () => getMyNotifications(),
    // A slow safety net, not the mechanism. The server pushes `notifications.raised` the moment a
    // notification is written, so this only covers a member whose socket never opened — a proxy
    // that blocks WebSockets, or an API that was down when the page loaded. Five minutes rather
    // than the minute it used to be, because the common case no longer waits for it at all.
    refetchInterval: 300_000,
  });

  useAnnounceNewNotifications(feed.data?.items);

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['notifications'] });

  const markAll = useMutation({ mutationFn: markAllNotificationsRead, onSuccess: refresh });
  const markOne = useMutation({ mutationFn: markNotificationRead, onSuccess: refresh });

  const clear = useMutation({
    mutationFn: clearNotifications,
    onSuccess: async () => {
      setConfirmingClear(false);
      setAnchor(null);
      await refresh();
    },
  });

  const open = (notification: Notification) => {
    if (!notification.isRead) {
      markOne.mutate(notification.id);
    }

    if (notification.route) {
      setAnchor(null);
      navigate(notification.route);
    }
  };

  const allMuted = (feed.data?.mutedFamilies.length ?? 0) >= ALL_FAMILIES;
  const items = feed.data?.items ?? [];

  return (
    <>
      <IconButton onClick={(event) => setAnchor(event.currentTarget)} aria-label="Notifications">
        <Badge
          // The server's number. Not items.length — see the note above.
          badgeContent={feed.data?.unreadCount ?? 0}
          color="error"
          max={99}
        >
          <MaterialSymbol name={allMuted ? 'notifications_off' : 'notifications'} size={22} />
        </Badge>
      </IconButton>

      <Popover
        open={anchor !== null}
        anchorEl={anchor}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{ paper: { sx: { width: { xs: 320, sm: 400 } } } }}
      >
        <Stack sx={{ px: 2, py: 1.5 }} spacing={0.25}>
          <Typography variant="subtitle2">Notifications</Typography>
          <Typography variant="caption" color="text.secondary">
            {feed.data?.unreadCount
              ? `${feed.data.unreadCount} unread`
              : 'Everything is read'}
          </Typography>
        </Stack>

        <Divider />

        {items.length === 0 ? (
          <Stack sx={{ px: 3, py: 4, alignItems: 'center' }} spacing={1}>
            <MaterialSymbol
              name={allMuted ? 'notifications_off' : 'inbox'}
              size={32}
              sx={{ color: 'text.disabled' }}
            />
            <Typography variant="subtitle2">
              {allMuted ? ALL_MUTED_TITLE : EMPTY_TITLE}
            </Typography>
            <Typography variant="caption" color="text.secondary" sx={{ textAlign: 'center' }}>
              {allMuted ? ALL_MUTED_BODY : EMPTY_BODY}
            </Typography>
          </Stack>
        ) : (
          <List disablePadding sx={{ maxHeight: 420, overflowY: 'auto' }}>
            {items.map((notification) => (
              <ListItemButton
                key={notification.id}
                onClick={() => open(notification)}
                sx={{
                  // Unread is a tint rather than a dot: the whole row is the thing that changed.
                  bgcolor: notification.isRead ? 'transparent' : 'action.hover',
                  alignItems: 'flex-start',
                }}
              >
                <ListItemIcon sx={{ mt: 0.5 }}>
                  <MaterialSymbol
                    name={KIND_ICON[notification.kind]}
                    size={20}
                    sx={{ color: `${KIND_COLOR[notification.kind]}.main` }}
                  />
                </ListItemIcon>
                <ListItemText
                  primary={notification.title}
                  secondary={notification.body}
                  slotProps={{
                    primary: {
                      variant: 'body2',
                      sx: { fontWeight: notification.isRead ? 400 : 600 },
                    },
                    secondary: { variant: 'caption' },
                  }}
                />
              </ListItemButton>
            ))}
          </List>
        )}

        <Divider />

        <Stack direction="row" sx={{ justifyContent: 'space-between', p: 1 }}>
          <Button
            size="small"
            startIcon={<MaterialSymbol name="tune" size={18} />}
            onClick={() => {
              setAnchor(null);
              navigate('/settings/notifications');
            }}
          >
            Settings
          </Button>
          <Stack direction="row" spacing={1}>
            <Button
              size="small"
              disabled={!feed.data?.unreadCount}
              loading={markAll.isPending}
              onClick={() => markAll.mutate()}
            >
              Mark all read
            </Button>
            <Button
              size="small"
              color="error"
              disabled={items.length === 0}
              onClick={() => setConfirmingClear(true)}
            >
              Clear all
            </Button>
          </Stack>
        </Stack>
      </Popover>

      <ConfirmDialog
        open={confirmingClear}
        title={CLEAR_CONFIRM_TITLE}
        description={CLEAR_CONFIRM_BODY}
        confirmLabel="Clear"
        destructive
        busy={clear.isPending}
        onConfirm={() => clear.mutate()}
        onCancel={() => setConfirmingClear(false)}
      />
    </>
  );
};

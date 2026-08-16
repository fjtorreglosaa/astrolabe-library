import { Avatar, Divider, IconButton, ListItemIcon, Menu, MenuItem, Stack, Typography } from '@mui/material';
import { MaterialSymbol } from '../shared/components/MaterialSymbol';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/components/AuthProvider';

/** The account menu: profile, settings, devices and sign out. */
export const UserMenu = () => {
  const { user, signOut } = useAuth();
  const navigate = useNavigate();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  if (!user) {
    return null;
  }

  const initials = user.fullName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('');

  const go = (path: string) => {
    setAnchor(null);
    navigate(path);
  };

  return (
    <>
      <IconButton onClick={(event) => setAnchor(event.currentTarget)} aria-label="Account menu">
        <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', fontSize: 14 }}>
          {initials}
        </Avatar>
      </IconButton>

      <Menu anchorEl={anchor} open={Boolean(anchor)} onClose={() => setAnchor(null)}>
        <Stack sx={{ px: 2, py: 1 }}>
          <Typography variant="subtitle2">{user.fullName}</Typography>
          <Typography variant="caption" color="text.secondary">
            {user.email}
          </Typography>
          {/* A member sees their plan and staff see their role. Showing "Member" to somebody who
              pays for Max would tell them nothing they did not know and hide the thing they did. */}
          <Typography variant="overline" color="primary.main">
            {user.isStaff ? user.role : (user.plan ?? 'Member')}
          </Typography>
        </Stack>

        <Divider />

        <MenuItem onClick={() => go('/profile')}>
          <ListItemIcon><MaterialSymbol name="person" size={18} /></ListItemIcon>
          My profile
        </MenuItem>
        <MenuItem onClick={() => go('/settings')}>
          <ListItemIcon><MaterialSymbol name="tune" size={18} /></ListItemIcon>
          Settings
        </MenuItem>
        <MenuItem onClick={() => go('/settings/devices')}>
          <ListItemIcon><MaterialSymbol name="devices" size={18} /></ListItemIcon>
          Devices and sessions
        </MenuItem>
        {/* Staff hold no plan, so the entry would only ever lead to an error for them. */}
        {user.isStaff ? null : (
          <MenuItem onClick={() => go('/settings/membership')}>
            <ListItemIcon><MaterialSymbol name="workspace_premium" size={18} /></ListItemIcon>
            Membership
          </MenuItem>
        )}

        <Divider />

        <MenuItem
          onClick={async () => {
            setAnchor(null);
            await signOut();
            navigate('/login', { replace: true });
          }}
          sx={{ color: 'error.main' }}
        >
          <ListItemIcon><MaterialSymbol name="logout" size={18} /></ListItemIcon>
          Sign out
        </MenuItem>
      </Menu>
    </>
  );
};

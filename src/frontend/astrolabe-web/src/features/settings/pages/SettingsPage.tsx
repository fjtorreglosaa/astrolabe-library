import {
  Divider,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Paper,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { MaterialSymbol } from '../../../shared/components/MaterialSymbol';
import { useUiStore } from '../../../app/uiStore';
import { useAuth } from '../../auth/components/AuthProvider';
import { PaymentMethodsPanel } from '../components/PaymentMethodsPanel';

/**
 * Settings.
 *
 * <p>
 * A hub rather than one long form. Three of these sections already had screens of their own —
 * membership, devices, notifications — and rebuilding them here would be two places to change one
 * rule. What lives inline is what has nowhere else to be: the theme, and the cards on file.
 * </p>
 */
export const SettingsPage = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const isStaff = user?.isStaff ?? false;
  const colorScheme = useUiStore((state) => state.colorScheme);
  const setColorScheme = useUiStore((state) => state.setColorScheme);

  const links = [
    {
      icon: 'workspace_premium',
      label: 'Membership',
      note: 'Your plan, what it includes, and changing it.',
      route: '/settings/membership',
      memberOnly: true,
    },
    {
      icon: 'notifications',
      label: 'Notification settings',
      note: 'What reaches the bell in the header.',
      route: '/settings/notifications',
      memberOnly: false,
    },
    {
      icon: 'devices',
      label: 'Devices and sessions',
      note: 'Everywhere your account is signed in.',
      route: '/settings/devices',
      memberOnly: false,
    },
    {
      icon: 'auto_awesome',
      label: 'AI recommendations',
      note: 'Provider keys for the libraries you administer.',
      route: '/admin/ai',
      staffOnly: true,
    },
  ].filter((link) => (link.staffOnly ? isStaff : link.memberOnly ? !isStaff : true));

  return (
    <Stack spacing={4}>
      <Stack spacing={0.5}>
        <Typography variant="h4">Settings</Typography>
        <Typography variant="body2" color="text.secondary">
          How the app looks, how it reaches you, and how you pay.
        </Typography>
      </Stack>

      <Stack spacing={2}>
        <Stack spacing={0.25}>
          <Typography variant="h6">Appearance</Typography>
          <Typography variant="body2" color="text.secondary">
            Choose the interface theme.
          </Typography>
        </Stack>
        <ToggleButtonGroup
          exclusive
          value={colorScheme}
          onChange={(_event, value) => value && setColorScheme(value)}
        >
          <ToggleButton value="light">
            <MaterialSymbol name="light_mode" size={20} />
            <Typography variant="body2" sx={{ ml: 1 }}>
              Light
            </Typography>
          </ToggleButton>
          <ToggleButton value="dark">
            <MaterialSymbol name="dark_mode" size={20} />
            <Typography variant="body2" sx={{ ml: 1 }}>
              Dark
            </Typography>
          </ToggleButton>
        </ToggleButtonGroup>
      </Stack>

      <Divider />

      {/* Members only: staff hold no cards here, and offering the section would be offering a
          dead end. */}
      {isStaff ? null : (
        <>
          <PaymentMethodsPanel />
          <Divider />
        </>
      )}

      <Stack spacing={2}>
        <Typography variant="h6">Everything else</Typography>
        <Paper variant="outlined">
          <List disablePadding>
            {links.map((link, index) => (
              <ListItemButton
                key={link.route}
                divider={index < links.length - 1}
                onClick={() => navigate(link.route)}
              >
                <ListItemIcon>
                  <MaterialSymbol name={link.icon} size={22} />
                </ListItemIcon>
                <ListItemText primary={link.label} secondary={link.note} />
                <MaterialSymbol name="chevron_right" size={20} />
              </ListItemButton>
            ))}
          </List>
        </Paper>
      </Stack>
    </Stack>
  );
};

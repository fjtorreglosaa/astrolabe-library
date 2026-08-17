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
import { AiSettingsCard } from '../components/AiSettingsCard';
import { MemberDefaultsPanel } from '../components/MemberDefaultsPanel';
import { MembershipSummaryCard } from '../components/MembershipSummaryCard';

/**
 * Settings.
 *
 * <p>
 * The prototype's order: appearance, membership, AI, the defaults for delivery and returns and
 * purchases, then everything that has a screen of its own.
 * </p>
 * <p>
 * Where a section already has its own screen — membership, notifications, devices, the provider
 * keys — what appears here is a <b>summary and a way through</b>, never a second copy of the form.
 * Two places that can change one plan is two places for its rules to drift apart. What lives inline
 * is what has nowhere else to be: the theme, the cards on file, and the three defaults.
 * </p>
 */
export const SettingsPage = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const isStaff = user?.isStaff ?? false;
  const colorScheme = useUiStore((state) => state.colorScheme);
  const setColorScheme = useUiStore((state) => state.setColorScheme);

  // Membership and AI are not here: each has a card of its own above. A section that appears twice
  // on one page is a section somebody will change in the wrong copy.
  const links = [
    {
      icon: 'notifications',
      label: 'Notification centre',
      note: 'What reaches the bell in the header, and whether it makes a sound.',
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
      icon: 'person',
      label: 'My profile',
      note: 'Your plan, your topics and your account statement.',
      route: '/profile',
      memberOnly: true,
    },
    {
      icon: 'support_agent',
      label: 'Help & support',
      note: 'Open a ticket and an agent from your library answers it.',
      route: '/support',
      memberOnly: false,
    },
  ].filter((link) => (link.memberOnly ? !isStaff : true));

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

      {/* Members only, all three: staff hold no plan, no cards and no reservations of their own, so
          each of these would be a dead end rather than an empty state. */}
      {isStaff ? null : (
        <>
          <MembershipSummaryCard />
          <Divider />
        </>
      )}

      <AiSettingsCard />

      <Divider />

      {isStaff ? null : (
        <>
          <MemberDefaultsPanel />
          <Divider />
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

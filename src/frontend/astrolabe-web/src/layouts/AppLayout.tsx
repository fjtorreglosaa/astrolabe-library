import {
  AppBar,
  Box,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  ListSubheader,
  Stack,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material';
import { MaterialSymbol } from '../shared/components/MaterialSymbol';
import { NavLink, Outlet } from 'react-router-dom';
import { useUiStore } from '../app/uiStore';
import { fonts, radii, rem, typeScale } from '../theme/tokens';
import { sectionsFor } from '../routes/navigation';
import { useAuth } from '../features/auth/components/AuthProvider';
import { UserMenu } from './UserMenu';
import { AppFooter } from './AppFooter';
import { NotificationBell } from '../features/notifications/components/NotificationBell';

const DRAWER_WIDTH = 264;

/**
 * The authenticated shell: top navbar, left sidebar, content, footer — the structure the prototype
 * uses on every signed-in screen.
 *
 * Sidebar entries are filtered by role in Stage 1, once identity exists. Until then every section
 * is rendered so the shell can be reviewed against the prototype.
 */
export const AppLayout = () => {
  const sidebarOpen = useUiStore((state) => state.sidebarOpen);
  const toggleSidebar = useUiStore((state) => state.toggleSidebar);
  const colorScheme = useUiStore((state) => state.colorScheme);
  const toggleColorScheme = useUiStore((state) => state.toggleColorScheme);
  const { role, plan } = useAuth();

  // Composed from the caller's role and plan: the role keeps administration entries away from a
  // member, and the plan keeps paid surfaces away from a member who has not bought them.
  const sections = role ? sectionsFor(role, plan) : [];

  const drawerContent = (
    <Box role="navigation" aria-label="Main navigation" sx={{ overflowY: 'auto' }}>
      {sections.map((section) => (
        <List
          key={section.label}
          dense
          subheader={
            <ListSubheader disableSticky sx={{ bgcolor: 'transparent', fontWeight: 700 }}>
              {section.label}
            </ListSubheader>
          }
        >
          {section.items.map((item) => (
            <ListItemButton key={item.route} component={NavLink} to={item.route} sx={{ mx: 1 }}>
              <ListItemIcon>
                <MaterialSymbol name={item.icon} size={20} />
              </ListItemIcon>
              <ListItemText primary={item.label} />
            </ListItemButton>
          ))}
        </List>
      ))}
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1 }}>
        <Toolbar>
          <IconButton edge="start" onClick={toggleSidebar} aria-label="Toggle navigation" sx={{ mr: 1 }}>
            <MaterialSymbol name="menu" size={22} />
          </IconButton>

          <Stack direction="row" spacing={1} sx={{ flexGrow: 1, alignItems: 'center' }}>
            <Box
              aria-hidden
              sx={{
                width: 30,
                height: 30,
                borderRadius: `${radii.tight}px`,
                bgcolor: 'primary.main',
                color: 'primary.contrastText',
                display: 'grid',
                placeItems: 'center',
                fontFamily: fonts.display,
                fontSize: rem(typeScale.lead),
                fontWeight: 600,
              }}
            >
              A
            </Box>
            <Typography variant="h4">Astrolabe Books</Typography>
          </Stack>

          <Tooltip title={colorScheme === 'light' ? 'Switch to dark theme' : 'Switch to light theme'}>
            <IconButton
              onClick={toggleColorScheme}
              aria-label={colorScheme === 'light' ? 'Switch to dark theme' : 'Switch to light theme'}
            >
              <MaterialSymbol name={colorScheme === 'light' ? 'dark_mode' : 'light_mode'} size={20} />
            </IconButton>
          </Tooltip>

          <NotificationBell />
            <UserMenu />
        </Toolbar>
      </AppBar>

      <Drawer
        variant="persistent"
        open={sidebarOpen}
        sx={{
          width: sidebarOpen ? DRAWER_WIDTH : 0,
          flexShrink: 0,
          '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box' },
        }}
      >
        <Toolbar />
        <Divider />
        {drawerContent}
      </Drawer>

      <Box sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <Toolbar />
        <Box component="main" sx={{ flexGrow: 1, p: 3 }}>
          <Outlet />
        </Box>
        <AppFooter />
      </Box>
    </Box>
  );
};

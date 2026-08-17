import {
  Box,
  Drawer,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { MaterialSymbol } from '../shared/components/MaterialSymbol';
import { Outlet, useLocation } from 'react-router-dom';
import { useUiStore } from '../app/uiStore';
import { fonts, radii, rem, typeScale } from '../theme/tokens';
import { sectionsFor } from '../routes/navigation';
import { pageTitleFor } from '../routes/pageTitle';
import { useAuth } from '../features/auth/components/AuthProvider';
import { UserMenu } from './UserMenu';
import { RealtimeIndicator } from './RealtimeIndicator';
import { AppFooter } from './AppFooter';
import { NotificationBell } from '../features/notifications/components/NotificationBell';
import { QuickActions } from './QuickActions';
import { SidebarAiCard } from './SidebarAiCard';
import { SidebarNav } from './SidebarNav';

const DRAWER_WIDTH = 264;

/**
 * The collapsed rail. The prototype narrows the sidebar to 78px rather than hiding it, so the icons
 * stay reachable — a navigation that disappears makes every jump a two-step act.
 */
const RAIL_WIDTH = 78;

/**
 * The content column's width, from the prototype's `<main max-width:1320px; margin:0 auto>`.
 *
 * A ceiling, not a width: below it the column simply fills the space. Above it the content stays
 * centred rather than stretching, because a table line that runs the full width of a wide monitor
 * is one the eye loses its place in halfway across.
 */
const CONTENT_MAX_WIDTH = 1320;

/**
 * Every control in the header, at the prototype's 40x40.
 *
 * <p>
 * Stated rather than inherited. An icon button left to its own devices takes its box from the
 * padding plus whatever font-size the glyph inside it resolves to — and the Material Symbols
 * stylesheet declares `font-size: 24px` on the same class our own rule targets, so which one wins
 * depends on stylesheet order rather than on anything in this file. Fixing the box makes the header
 * the same height whichever rule lands on top.
 * </p>
 */
const HEADER_BUTTON = { width: 40, height: 40, flexShrink: 0 } as const;

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
  const { pathname } = useLocation();

  // Composed from the caller's role and plan: the role keeps administration entries away from a
  // member, and the plan keeps paid surfaces away from a member who has not bought them.
  const sections = role ? sectionsFor(role, plan) : [];

  const drawerContent = (
    <Stack sx={{ height: '100%' }}>
      <SidebarNav sections={sections} expanded={sidebarOpen} />

      {/* Pinned to the foot, where the prototype puts it. */}
      <SidebarAiCard collapsed={!sidebarOpen} />
    </Stack>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: 'background.default' }}>

      <Drawer
        variant="permanent"
        sx={{
          width: sidebarOpen ? DRAWER_WIDTH : RAIL_WIDTH,
          flexShrink: 0,
          whiteSpace: 'nowrap',
          transition: (theme) => theme.transitions.create('width'),
          '& .MuiDrawer-paper': {
            width: sidebarOpen ? DRAWER_WIDTH : RAIL_WIDTH,
            boxSizing: 'border-box',
            overflowX: 'hidden',
            transition: (theme) => theme.transitions.create('width'),
          },
        }}
      >
        {/*
          The brand lives here and only here, in the prototype's own 64px sidebar head — the same
          height as the header beside it, so the two rules line up across the fold.
        */}
        <Stack
          direction="row"
          spacing={1.25}
          sx={{
            height: 64,
            flexShrink: 0,
            px: sidebarOpen ? 2.5 : 0,
            alignItems: 'center',
            justifyContent: sidebarOpen ? 'flex-start' : 'center',
            borderBottom: 1,
            borderColor: 'divider',
          }}
        >
          <Box
            aria-hidden
            sx={{
              width: 32,
              height: 32,
              flexShrink: 0,
              borderRadius: `${radii.tight}px`,
              bgcolor: 'primary.main',
              color: 'primary.contrastText',
              display: 'grid',
              placeItems: 'center',
              fontFamily: fonts.display,
              fontSize: rem(typeScale.title),
              fontWeight: 700,
            }}
          >
            A
          </Box>
          {sidebarOpen ? (
            <Typography variant="h5" noWrap>
              Astrolabe Books
            </Typography>
          ) : null}
        </Stack>

        {drawerContent}
      </Drawer>

      <Box sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        {/*
          The header sits *inside* the content column, beside the sidebar rather than above it, and
          it is the page title that goes here — the prototype's `min-height:64px; gap:14px;
          padding:0 20px`, with 40x40 controls.

          Every control is sized explicitly. Leaving them to the defaults meant an icon button whose
          height came from whatever font-size the icon span happened to inherit, which is how they
          ended up out of proportion with a 64px bar.
        */}
        <Stack
          component="header"
          direction="row"
          spacing={1.75}
          sx={{
            minHeight: 64,
            flexShrink: 0,
            px: 2.5,
            alignItems: 'center',
            borderBottom: 1,
            borderColor: 'divider',
            bgcolor: 'background.paper',
            position: 'sticky',
            top: 0,
            zIndex: (theme) => theme.zIndex.appBar,
          }}
        >
          <Tooltip title="Toggle navigation">
            <IconButton onClick={toggleSidebar} aria-label="Toggle navigation" sx={HEADER_BUTTON}>
              <MaterialSymbol name={sidebarOpen ? 'menu_open' : 'menu'} size={22} />
            </IconButton>
          </Tooltip>

          <Typography variant="h4" noWrap sx={{ minWidth: 0 }}>
            {pageTitleFor(pathname)}
          </Typography>

          <Box sx={{ flex: 1 }} />

          <Tooltip title={colorScheme === 'light' ? 'Switch to dark theme' : 'Switch to light theme'}>
            <IconButton
              onClick={toggleColorScheme}
              aria-label={colorScheme === 'light' ? 'Switch to dark theme' : 'Switch to light theme'}
              sx={HEADER_BUTTON}
            >
              <MaterialSymbol name={colorScheme === 'light' ? 'dark_mode' : 'light_mode'} size={22} />
            </IconButton>
          </Tooltip>

          {/* Before the bell: it explains why the bell might be quiet. */}
          <RealtimeIndicator />
          <NotificationBell />
          <UserMenu />
        </Stack>
        {/*
          The prototype's own content column:
          `flex:1; width:100%; max-width:1320px; margin:0 auto; padding:28px; overflow-x:hidden`.

          The centring is the part that was missing. Without a maximum width the content stretched
          to the full width of the window, which on a wide screen turns a table into lines the eye
          has to track across half a metre — and left every screen looking unlike the mockup at the
          size most people actually use.

          `minWidth: 0` lets a wide table shrink inside the flex column instead of pushing the
          layout out; `overflowX: hidden` is the prototype's matching guard.
        */}
        <Box
          component="main"
          sx={{
            flexGrow: 1,
            width: '100%',
            maxWidth: CONTENT_MAX_WIDTH,
            mx: 'auto',
            p: 3.5,
            minWidth: 0,
            overflowX: 'hidden',
          }}
        >
          {/* Keyed on the path so the transition replays on each navigation, as `fadeUp .2s
              ease-out` does in the prototype. Skipped for a reader who asked for less motion. */}
          <Box
            key={pathname}
            sx={{
              '@media (prefers-reduced-motion: no-preference)': {
                animation: 'astrolabe-fade-up .2s ease-out',
              },
              '@keyframes astrolabe-fade-up': {
                from: { opacity: 0, transform: 'translateY(8px)' },
                to: { opacity: 1, transform: 'translateY(0)' },
              },
            }}
          >
            <Outlet />
          </Box>
        </Box>
        <AppFooter />
      </Box>

      <QuickActions />
    </Box>
  );
};

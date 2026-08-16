import { Backdrop, Box, IconButton, SpeedDial, SpeedDialAction, Tooltip } from '@mui/material';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { MaterialSymbol } from '../shared/components/MaterialSymbol';
import { useUiStore } from '../app/uiStore';
import { useAuth } from '../features/auth/components/AuthProvider';

interface QuickAction {
  icon: string;
  label: string;
  route: string;
}

/**
 * The four a member reaches for. Transcribed from the prototype's `quick` list.
 *
 * Every one is a shortcut to a screen they can already reach — the value is that it is the same
 * gesture from anywhere, not that it unlocks anything.
 */
const MEMBER_ACTIONS: QuickAction[] = [
  { icon: 'qr_code_scanner', label: 'Quick check-in', route: '/loans' },
  { icon: 'search', label: 'Search catalogue', route: '/catalog' },
  { icon: 'local_shipping', label: 'Delivery status', route: '/loans' },
  { icon: 'payments', label: 'Pay fines', route: '/fines' },
];

const STAFF_ACTIONS: QuickAction[] = [
  { icon: 'group', label: 'Users', route: '/admin/users' },
  { icon: 'library_add', label: 'Book management', route: '/admin/books' },
  { icon: 'auto_awesome', label: 'AI settings', route: '/admin/ai' },
];

/**
 * Quick actions.
 *
 * <p>
 * Two behaviours, not one, and the prototype is explicit about both: the dial opens and closes, and
 * the button itself <b>docks</b> — put away with a dismiss and brought back from a small handle. A
 * floating button that cannot be moved out of the way is one that eventually covers the thing
 * somebody is trying to read, and on a phone that is most of the screen.
 * </p>
 * <p>
 * The docked state is persisted. Somebody who dismissed it meant it, and returning it on every
 * navigation would be arguing with them.
 * </p>
 */
export const QuickActions = () => {
  const navigate = useNavigate();
  const { role } = useAuth();
  const docked = useUiStore((state) => state.quickActionsDocked);
  const setDocked = useUiStore((state) => state.setQuickActionsDocked);
  const [open, setOpen] = useState(false);

  const isStaff = role === 'Admin' || role === 'SuperAdmin';
  const actions = isStaff ? STAFF_ACTIONS : MEMBER_ACTIONS;

  if (docked) {
    return (
      <Tooltip title="Show quick actions" placement="left">
        <IconButton
          aria-label="Show quick actions"
          onClick={() => setDocked(false)}
          sx={{
            position: 'fixed',
            right: 0,
            bottom: 96,
            zIndex: (theme) => theme.zIndex.speedDial,
            // A handle rather than a button: flush to the edge and half-hidden, so it is findable
            // without competing with the page.
            borderRadius: '8px 0 0 8px',
            bgcolor: 'background.paper',
            border: 1,
            borderRight: 0,
            borderColor: 'divider',
            boxShadow: 2,
          }}
        >
          <MaterialSymbol name="bolt" size={20} />
        </IconButton>
      </Tooltip>
    );
  }

  return (
    <>
      {/* Dims the page while the dial is open, so the four choices are the only thing to read. */}
      <Backdrop open={open} sx={{ zIndex: (theme) => theme.zIndex.speedDial - 1 }} />

      <Box sx={{ position: 'fixed', right: 24, bottom: 24, zIndex: (theme) => theme.zIndex.speedDial }}>
        <SpeedDial
          ariaLabel="Quick actions"
          open={open}
          onOpen={() => setOpen(true)}
          onClose={() => setOpen(false)}
          icon={<MaterialSymbol name={open ? 'close' : 'bolt'} size={24} />}
        >
          {actions.map((action) => (
            <SpeedDialAction
              key={action.label}
              icon={<MaterialSymbol name={action.icon} size={20} />}
              slotProps={{ tooltip: { title: action.label, open: true } }}
              onClick={() => {
                setOpen(false);
                navigate(action.route);
              }}
            />
          ))}

          {/* Dismissing is one of the actions rather than a separate control, so the gesture that
              opened the dial is the gesture that puts it away. */}
          <SpeedDialAction
            icon={<MaterialSymbol name="close_fullscreen" size={20} />}
            slotProps={{ tooltip: { title: 'Hide this button', open: true } }}
            onClick={() => {
              setOpen(false);
              setDocked(true);
            }}
          />
        </SpeedDial>
      </Box>
    </>
  );
};

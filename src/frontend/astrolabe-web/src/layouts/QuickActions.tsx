import {
  Box,
  ClickAwayListener,
  Divider,
  Fab,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Paper,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { MaterialSymbol } from '../shared/components/MaterialSymbol';
import { useUiStore } from '../app/uiStore';
import { useAuth } from '../features/auth/components/AuthProvider';
import { MEMBER_ACTIONS, STAFF_ACTIONS } from './quickActionItems';

/**
 * Quick actions.
 *
 * <p>
 * A labelled panel, not a speed dial. The prototype opens a card above the button carrying a `bolt`
 * icon, the heading <b>Quick actions</b>, the line <b>Jump straight to what you need.</b> and the
 * actions as full rows with their labels always visible. That matters: a dial shows its labels only
 * on hover, which on a touch screen means it shows them never.
 * </p>
 * <p>
 * Two separate behaviours, both from the prototype. The button <b>opens</b> the panel — its icon
 * turns from `bolt` to `close`. And a `keyboard_tab` control in the panel header <b>docks</b> the
 * whole thing away to the screen edge, from where the same icon brings it back. A floating button
 * that cannot be dismissed eventually covers whatever somebody is trying to read.
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

  // Docked: only the handle remains, flush to the edge. The same `keyboard_tab` icon that put it
  // away brings it back, so the gesture reads as one thing in two directions.
  if (docked) {
    return (
      <Tooltip title="Show quick actions" placement="left">
        <IconButton
          aria-label="Show quick actions"
          onClick={() => {
            setDocked(false);
            setOpen(true);
          }}
          sx={{
            position: 'fixed',
            right: 0,
            bottom: 88,
            zIndex: (theme) => theme.zIndex.speedDial,
            borderRadius: '10px 0 0 10px',
            bgcolor: 'background.paper',
            border: 1,
            borderRight: 0,
            borderColor: 'divider',
            boxShadow: 3,
            '&:hover': { bgcolor: 'background.paper' },
          }}
        >
          <MaterialSymbol name="keyboard_tab" size={20} sx={{ transform: 'rotate(180deg)' }} />
        </IconButton>
      </Tooltip>
    );
  }

  return (
    <ClickAwayListener onClickAway={() => setOpen(false)}>
      <Box
        sx={{
          position: 'fixed',
          right: 24,
          bottom: 24,
          zIndex: (theme) => theme.zIndex.speedDial,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'flex-end',
          gap: 1.5,
        }}
      >
        {open ? (
          <Paper
            elevation={8}
            sx={{ width: 268, overflow: 'hidden', borderRadius: '12px' }}
            role="menu"
            aria-label="Quick actions"
          >
            <Stack
              direction="row"
              spacing={1}
              sx={{ p: 2, pb: 1.5, alignItems: 'flex-start' }}
            >
              <MaterialSymbol name="bolt" size={22} sx={{ color: 'primary.main', mt: 0.25 }} />
              <Stack spacing={0.25} sx={{ flex: 1, minWidth: 0 }}>
                <Typography variant="subtitle2">Quick actions</Typography>
                <Typography variant="caption" color="text.secondary">
                  Jump straight to what you need.
                </Typography>
              </Stack>
              <Tooltip title="Hide this button">
                <IconButton
                  size="small"
                  aria-label="Hide quick actions"
                  onClick={() => {
                    setOpen(false);
                    setDocked(true);
                  }}
                >
                  <MaterialSymbol name="keyboard_tab" size={18} />
                </IconButton>
              </Tooltip>
            </Stack>

            <Divider />

            <List disablePadding>
              {actions.map((action) => (
                <ListItemButton
                  key={action.label}
                  onClick={() => {
                    setOpen(false);
                    navigate(action.route);
                  }}
                >
                  <ListItemIcon sx={{ minWidth: 40 }}>
                    <MaterialSymbol name={action.icon} size={20} />
                  </ListItemIcon>
                  {/* The label is always on screen. On a touch device a hover tooltip is a label
                      that never appears. */}
                  <ListItemText primary={action.label} slotProps={{ primary: { variant: 'body2' } }} />
                </ListItemButton>
              ))}
            </List>
          </Paper>
        ) : null}

        <Fab
          color="primary"
          aria-label={open ? 'Close quick actions' : 'Quick actions'}
          aria-expanded={open}
          onClick={() => setOpen(!open)}
        >
          <MaterialSymbol name={open ? 'close' : 'bolt'} size={24} />
        </Fab>
      </Box>
    </ClickAwayListener>
  );
};

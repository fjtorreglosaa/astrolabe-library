import { Box, ListItemButton, ListItemIcon, ListItemText, Tooltip, Typography } from '@mui/material';
import { NavLink } from 'react-router-dom';
import { MaterialSymbol } from '../shared/components/MaterialSymbol';
import { rem, typeScale } from '../theme/tokens';
import type { NavigationSection } from '../routes/navigation';

/** The prototype sets sidebar entries one step above body text, at 14px. */
const ITEM_FONT_PX = 14;

/**
 * The sidebar's navigation, at the prototype's own box model.
 *
 * <pre>
 *   nav     padding:12px; display:flex; flex-direction:column; gap:2px
 *   header  padding:18px 12px 6px; font-size:11px; letter-spacing:.12em; uppercase
 *   item    height:44px; border-radius:22px; gap:14px; padding:0 12px; font-size:14px; icon 21px
 * </pre>
 *
 * <p>
 * <b>`flexGrow: 0` on the item is the load-bearing line.</b> MUI gives `ListItemButton` a
 * `flex-grow: 1` of its own, because it expects to sit inside a `ListItem` — a <em>row</em> — and
 * fill its width. Placed directly in this column instead, that same rule applies down the cross
 * axis: every entry stretches to fill the sidebar's height and the whole navigation ends up spread
 * evenly from the top of the panel to the bottom, which is nothing like the mockup and looks like a
 * spacing mistake rather than an inherited default.
 * </p>
 * <p>
 * It did not happen while these were wrapped in `<List>` elements, because a `List` is a block and
 * `flex-grow` had nothing to act on. Removing the wrappers to get the prototype's 2px gap is what
 * exposed it — which is why the rule is stated here rather than left to a default, and why there is
 * a test holding it.
 * </p>
 */
export interface SidebarNavProps {
  sections: NavigationSection[];
  /** Wide, or narrowed to the icon rail. */
  expanded: boolean;
}

export const SidebarNav = ({ sections, expanded }: SidebarNavProps) => (
  <Box
    role="navigation"
    aria-label="Main navigation"
    sx={{
      flex: 1,
      overflowY: 'auto',
      p: 1.5,
      display: 'flex',
      flexDirection: 'column',
      // Stated rather than relied upon: the entries belong at the top of the panel, and whatever
      // height is left over stays empty.
      justifyContent: 'flex-start',
      alignContent: 'flex-start',
      gap: '2px',
    }}
  >
    {sections.map((section) => (
      <Box key={section.label} sx={{ display: 'contents' }}>
        {expanded ? (
          <Typography
            component="div"
            sx={{
              flex: 'none',
              pt: '18px',
              pb: '6px',
              px: '12px',
              fontSize: rem(typeScale.micro),
              letterSpacing: '.12em',
              textTransform: 'uppercase',
              fontWeight: 600,
              lineHeight: 1.2,
              color: 'text.secondary',
            }}
          >
            {section.label}
          </Typography>
        ) : null}

        {section.items.map((item) => (
          <Tooltip
            key={item.route}
            // Only in the rail. A tooltip repeating a label already on screen is noise.
            title={expanded ? '' : item.label}
            placement="right"
          >
            <ListItemButton
              component={NavLink}
              to={item.route}
              sx={{
                // See the note above: without these three the entries stretch down the panel.
                flexGrow: 0,
                flexShrink: 0,
                flexBasis: 'auto',

                height: 44,
                minHeight: 44,
                // MUI keeps 8px of vertical padding here and ListItemText adds 4px of margin, which
                // together overflow a 44px row — and `height` is not a cap, so it simply grows.
                py: 0,
                px: expanded ? '12px' : 0,
                borderRadius: '22px',
                gap: '14px',
                justifyContent: expanded ? 'flex-start' : 'center',
                fontSize: rem(ITEM_FONT_PX),
                '& .MuiListItemText-root': { my: 0 },
              }}
            >
              <ListItemIcon sx={{ minWidth: 0 }}>
                <MaterialSymbol name={item.icon} size={21} />
              </ListItemIcon>
              {expanded ? (
                <ListItemText
                  primary={item.label}
                  slotProps={{ primary: { noWrap: true, sx: { fontSize: rem(ITEM_FONT_PX) } } }}
                />
              ) : null}
            </ListItemButton>
          </Tooltip>
        ))}
      </Box>
    ))}
  </Box>
);

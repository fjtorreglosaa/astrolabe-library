import { buttonClasses } from '@mui/material/Button';
import { createTheme, type Theme } from '@mui/material/styles';
import {
  brand,
  elevation,
  fonts,
  label,
  palettes,
  radii,
  rem,
  semantic,
  typeScale,
  weights,
  type ColorScheme,
} from './tokens';

/**
 * Builds the Material UI theme from the prototype's measured tokens.
 *
 * Three corrections do most of the work of making Material UI look like the prototype:
 * the type scale is far denser (13px body, not 16px), weight 600 is the norm rather than the
 * exception, and small labels are uppercase with wide tracking.
 */
export const createAstrolabeTheme = (scheme: ColorScheme): Theme => {
  const p = palettes[scheme];

  return createTheme({
    palette: {
      mode: scheme,
      primary: { main: brand.primary, contrastText: '#FFFFFF' },
      background: { default: p.background, paper: p.surface },
      text: { primary: p.text, secondary: p.muted },
      divider: p.border,
      success: { main: semantic.success },
      warning: { main: semantic.warning },
      error: { main: semantic.error },
      info: { main: semantic.info },
      action: { selected: p.selected },
    },

    typography: {
      fontFamily: fonts.body,
      fontSize: typeScale.body,

      // Headings use Playfair at the sizes the prototype actually renders. Material UI's defaults
      // run from 96px down; nothing in the prototype exceeds 26px.
      h1: { fontFamily: fonts.display, fontWeight: weights.semibold, fontSize: rem(typeScale.display), letterSpacing: '-.01em' },
      h2: { fontFamily: fonts.display, fontWeight: weights.semibold, fontSize: rem(24) },
      h3: { fontFamily: fonts.display, fontWeight: weights.semibold, fontSize: rem(typeScale.heading) },
      h4: { fontFamily: fonts.display, fontWeight: weights.semibold, fontSize: rem(typeScale.title) },
      h5: { fontFamily: fonts.display, fontWeight: weights.semibold, fontSize: rem(typeScale.subtitle) },
      h6: { fontFamily: fonts.body, fontWeight: weights.semibold, fontSize: rem(typeScale.lead) },

      subtitle1: { fontWeight: weights.semibold, fontSize: rem(typeScale.bodyLarge) },
      subtitle2: { fontWeight: weights.semibold, fontSize: rem(typeScale.body) },

      body1: { fontSize: rem(typeScale.body), lineHeight: 1.55 },
      body2: { fontSize: rem(typeScale.small), lineHeight: 1.5 },

      caption: { fontSize: rem(typeScale.micro), lineHeight: 1.45 },

      // The prototype's signature: uppercase micro-labels with wide tracking, used for section
      // headers, table headers and status chips.
      overline: {
        fontSize: rem(label.size),
        fontWeight: label.weight,
        letterSpacing: label.tracking,
        textTransform: label.transform,
        lineHeight: 1.6,
      },

      button: {
        textTransform: 'none',
        fontWeight: weights.semibold,
        fontSize: rem(typeScale.body),
        letterSpacing: 0,
      },
    },

    shape: { borderRadius: radii.panel },

    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: { backgroundColor: p.background, color: p.text },

          // Material Symbols is a variable font: without these axes it renders at the wrong weight
          // and optical size, which is the most visible way an icon set looks "off".
          '.material-symbols-outlined': {
            fontFamily: fonts.icons,
            fontWeight: 'normal',
            fontStyle: 'normal',
            lineHeight: 1,
            letterSpacing: 'normal',
            textTransform: 'none',
            display: 'inline-block',
            whiteSpace: 'nowrap',
            wordWrap: 'normal',
            direction: 'ltr',
            fontVariationSettings: "'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24",
          },
        },
      },

      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: {
          /**
           * The prototype's button vocabulary, counted from its own markup:
           *
           *   54  outlined  border:1px solid {field}; background:transparent; color:inherit
           *   30  contained background:#0E5A6E; color:#fff
           *    0  outlined in the brand colour — there is not one anywhere
           *
           * So a secondary button is **neutral**, and the teal is reserved for the single primary
           * action on a surface. MUI's default is the opposite: an outlined button inherits
           * `color="primary"` and renders a teal border and teal text, which is why every Cancel,
           * Details and Reload on screen came out in the brand colour and read as green.
           */
          root: ({ ownerState }) => ({
            borderRadius: radii.round,
            paddingInline: 16,
            ...(ownerState.variant === 'outlined' &&
              ownerState.color === 'primary' && {
                color: p.text,
                borderColor: p.field,
                '&:hover': { borderColor: p.field, backgroundColor: p.selected },
              }),
          }),
          sizeSmall: { fontSize: rem(typeScale.small), paddingInline: 12 },
          sizeLarge: { fontSize: rem(typeScale.bodyLarge), paddingBlock: 10 },
          // The teal glow the prototype puts under its primary action. MUI v9 dropped the
          // per-variant slot keys, so the rule is scoped by class instead.
          contained: {
            [`&.${buttonClasses.colorPrimary}`]: {
              boxShadow: elevation.primary,
              '&:hover': { boxShadow: elevation.primary },
            },
          },
        },
      },

      MuiPaper: {
        defaultProps: { elevation: 0 },
        styleOverrides: {
          root: { backgroundImage: 'none' },
          // Cards are separated by a border, never by a drop shadow.
          outlined: { borderRadius: radii.panel, borderColor: p.border },
        },
      },

      MuiDialog: {
        styleOverrides: {
          paper: { borderRadius: radii.card, boxShadow: elevation.overlay },
        },
      },

      MuiMenu: {
        styleOverrides: {
          paper: { borderRadius: radii.panel, border: `1px solid ${p.border}`, boxShadow: elevation.overlay },
        },
      },

      MuiChip: {
        styleOverrides: {
          root: {
            borderRadius: radii.pill,
            fontWeight: weights.semibold,
            fontSize: rem(typeScale.small),
          },
          sizeSmall: { fontSize: rem(label.size), height: 22 },
        },
      },

      MuiOutlinedInput: {
        styleOverrides: {
          root: { borderRadius: radii.input, fontSize: rem(typeScale.body) },
          notchedOutline: { borderColor: p.field },
        },
      },

      MuiInputLabel: {
        styleOverrides: { root: { fontSize: rem(typeScale.body) } },
      },

      MuiFormHelperText: {
        styleOverrides: { root: { fontSize: rem(label.size), marginInline: 2 } },
      },

      MuiAppBar: {
        defaultProps: { elevation: 0, color: 'inherit' },
        styleOverrides: {
          root: { borderBottom: `1px solid ${p.border}`, backgroundColor: p.surface },
        },
      },

      MuiDrawer: {
        styleOverrides: {
          paper: { borderRight: `1px solid ${p.border}`, backgroundColor: p.surface },
        },
      },

      // Sidebar section headers carry the uppercase micro-label treatment.
      MuiListSubheader: {
        styleOverrides: {
          root: {
            backgroundColor: 'transparent',
            color: p.muted,
            fontSize: rem(label.size),
            fontWeight: label.weight,
            letterSpacing: label.tracking,
            textTransform: label.transform,
            lineHeight: 2.6,
          },
        },
      },

      MuiListItemButton: {
        styleOverrides: {
          root: {
            borderRadius: radii.input,
            fontSize: rem(typeScale.body),
            '&.Mui-selected, &.active': { backgroundColor: p.selected, color: brand.primary },
          },
        },
      },

      MuiListItemText: {
        styleOverrides: {
          primary: { fontSize: rem(typeScale.body), fontWeight: weights.semibold },
          secondary: { fontSize: rem(typeScale.small) },
        },
      },

      MuiListItemIcon: {
        styleOverrides: { root: { minWidth: 36, color: 'inherit' } },
      },

      MuiTableCell: {
        styleOverrides: {
          root: { fontSize: rem(typeScale.body), borderColor: p.border },
          head: {
            fontSize: rem(label.size),
            fontWeight: label.weight,
            letterSpacing: label.tracking,
            textTransform: label.transform,
            color: p.muted,
          },
        },
      },

      MuiAlert: {
        styleOverrides: {
          root: { borderRadius: radii.control, fontSize: rem(typeScale.small) },
        },
      },

      MuiTooltip: {
        styleOverrides: {
          tooltip: { fontSize: rem(label.size), borderRadius: radii.tight },
        },
      },

      MuiAvatar: {
        styleOverrides: {
          root: { fontSize: rem(typeScale.small), fontWeight: weights.semibold },
        },
      },
    },
  });
};

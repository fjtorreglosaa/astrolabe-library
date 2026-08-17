/**
 * Design tokens extracted from the approved prototype.
 *
 * Every value here was measured from the prototype's rendered DOM rather than eyeballed. The
 * prototype uses inline styles and no Material UI, so it is a visual reference rather than reusable
 * code — these values are the contract between it and our theme. See GUIDELINES.md section 38.1.
 */

export const brand = {
  /** Deep teal. Identical in both schemes by design. */
  primary: '#0E5A6E',
} as const;

export const palettes = {
  light: {
    background: '#F4F9FB',
    surface: '#FFFFFF',
    text: '#10262E',
    muted: '#5C7480',
    border: 'rgba(16,38,46,.12)',
    field: 'rgba(16,38,46,.26)',
    /** Subtle fill for a selected row or a resting hover. */
    selected: 'rgba(14,90,110,.14)',
  },
  dark: {
    background: '#0B1519',
    surface: '#10222A',
    text: '#E8F3F6',
    muted: '#93AFB9',
    border: 'rgba(255,255,255,.12)',
    field: 'rgba(255,255,255,.28)',
    selected: 'rgba(14,90,110,.30)',
  },
} as const;

export const semantic = {
  success: '#0F7A63',
  successAlt: '#0C7F70',
  successSurface: 'rgba(16,168,140,.14)',
  warning: '#8A6A28',
  warningSurface: 'rgba(224,166,60,.20)',
  error: '#B3261E',
  errorSurface: 'rgba(179,38,30,.12)',
  info: '#0E5A6E',
  infoSurface: 'rgba(14,90,110,.12)',
} as const;

/** Generated cover tints, applied when a book has no cover image. */
export const coverTints = [
  '#0E5A6E',
  '#0B2E3B',
  '#12766B',
  '#1F5F8B',
  '#0F8A7A',
  '#2A6E7E',
  '#164A5C',
  '#3A4E7A',
] as const;

export const fonts = {
  /** Brand and headings. Used at 18–26px, always weight 600. */
  display: "'Playfair Display', Georgia, serif",
  /** Interface text. */
  body: "'Plus Jakarta Sans', system-ui, -apple-system, sans-serif",
  /** Icon font. The prototype uses Material Symbols, not the filled Material Icons set. */
  icons: "'Material Symbols Outlined'",
} as const;

/**
 * The prototype's type scale, in pixels as measured.
 *
 * It is markedly denser than Material UI's defaults — body text is 13px where MUI ships 16px — which
 * is the single biggest reason an untuned MUI theme does not look like the prototype.
 */
export const typeScale = {
  micro: 11,
  small: 12,
  body: 13,
  bodyLarge: 14,
  lead: 15,
  subtitle: 18,
  title: 19,
  heading: 21,
  display: 26,
} as const;

/**
 * Weight 600 accounts for nearly every styled element in the prototype. Regular text is the
 * exception, not the rule, which is why the interface reads as dense and deliberate.
 */
export const weights = {
  regular: 400,
  medium: 500,
  semibold: 600,
  bold: 700,
} as const;

/**
 * Uppercase micro-labels with wide tracking are the prototype's strongest typographic signature:
 * section headers, table headers and status chips all use them.
 */
export const label = {
  size: typeScale.micro,
  weight: weights.semibold,
  tracking: '.14em',
  transform: 'uppercase',
} as const;

/**
 * Corner radii, taken from the prototype's own vocabulary.
 *
 * <p>
 * `panel` is the one that matters: `border-radius:12px` is the prototype's most common container
 * value by a wide margin — every stat card, book card, fines panel and payment card carries it.
 * Cards were being drawn at 16 here, which is the modal radius, and the difference showed up
 * everywhere at once.
 * </p>
 * <p>
 * <b>Write radii as pixel strings in `sx`.</b> A bare number there is multiplied by
 * `theme.shape.borderRadius` — twelve — so `borderRadius: 3` is thirty-six pixels, not the
 * twenty-four that thinking in spacing units suggests. Every radius in this codebase is therefore
 * stated in px.
 * </p>
 */
export const radii = {
  /** Cover thumbnails and other small blocks. */
  thumb: 4,
  tight: 6,
  /** Tinted strips, inline notices, code fields. */
  control: 8,
  /** Inputs, option cards, tiles. */
  input: 10,
  /** Cards and panels. The prototype's default container. */
  panel: 12,
  /** Modals. */
  card: 16,
  /** A 44px control. */
  pill: 22,
  /** Large enough to round any control to a pill, whatever its height. */
  round: 999,
} as const;

export const elevation = {
  /** Resting cards sit flat; the prototype separates them with borders, not shadows. */
  none: 'none',
  /** The teal glow under a primary action. */
  primary: '0 8px 20px rgba(14,90,110,.35)',
  /** Overlays: wide, soft and low-opacity rather than tight and dark. */
  overlay: '0 26px 64px rgba(0,0,0,.45)',
} as const;

/** Root font size the rem scale is computed against. */
const ROOT_FONT_SIZE = 16;

/**
 * Converts a measured pixel size to rem.
 *
 * Sizes are expressed in rem rather than px so the interface still scales for a reader who has
 * enlarged their browser's default font — a hard-coded px scale silently ignores that setting.
 */
export const rem = (pixels: number): string => `${pixels / ROOT_FONT_SIZE}rem`;

/** Picks a deterministic tint so a given book always renders the same colour. */
export const tintForId = (id: string | number): string => {
  const numeric = Number(String(id).replace(/\D/g, '')) || 0;
  return coverTints[(numeric - 1 + coverTints.length) % coverTints.length] ?? coverTints[0];
};

export type ColorScheme = keyof typeof palettes;

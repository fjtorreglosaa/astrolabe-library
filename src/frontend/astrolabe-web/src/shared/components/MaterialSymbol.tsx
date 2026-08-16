import { Box, type SxProps, type Theme } from '@mui/material';

/**
 * Renders a Material Symbols Outlined glyph.
 *
 * The prototype uses Material Symbols — the outlined variable font — not the filled Material Icons
 * set that `@mui/icons-material` ships. Mixing the two is immediately visible: the filled set reads
 * heavier and rounder, which is the main reason the interface looked unlike the mockup even with
 * the right colours.
 *
 * Icon names come straight from the prototype's own navigation definition.
 */
export interface MaterialSymbolProps {
  /** The glyph name, for example `space_dashboard`. */
  name: string;
  /** Optical size in pixels. The font exposes it as an axis, so it stays crisp at any size. */
  size?: number;
  /** 0 for outlined, 1 for filled. The prototype uses outlined throughout. */
  fill?: 0 | 1;
  weight?: 300 | 400 | 500 | 600 | 700;
  sx?: SxProps<Theme>;
}

export const MaterialSymbol = ({
  name,
  size = 20,
  fill = 0,
  weight = 400,
  sx,
}: MaterialSymbolProps) => (
  <Box
    component="span"
    className="material-symbols-outlined"
    aria-hidden
    sx={{
      fontSize: `${size}px`,
      // Set per instance so a single icon can be filled or heavier without a second font request.
      fontVariationSettings: `'FILL' ${fill}, 'wght' ${weight}, 'GRAD' 0, 'opsz' ${size}`,
      ...sx,
    }}
  >
    {name}
  </Box>
);

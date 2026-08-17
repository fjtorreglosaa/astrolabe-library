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
      // `&&` on purpose. The Material Symbols stylesheet from Google declares
      // `.material-symbols-outlined { font-size: 24px }` — the same class this component puts on
      // every glyph, at the same specificity as an emotion class. Which one wins then comes down to
      // which <style> lands later in <head>, which is not something this file controls.
      //
      // Doubling the selector takes it to (0,2,0) and settles it for good. Without this, an icon
      // asked for at 20px silently renders at 24, every button sized from its glyph grows with it,
      // and the whole shell drifts out of proportion for reasons nothing in the source explains.
      '&&': {
        fontSize: `${size}px`,
        // Set per instance so one icon can be filled or heavier without a second font request.
        fontVariationSettings: `'FILL' ${fill}, 'wght' ${weight}, 'GRAD' 0, 'opsz' ${size}`,
        // The same stylesheet also ships `line-height: 1`; stated here so the glyph box is exactly
        // the size asked for rather than a line box built from the inherited leading.
        lineHeight: 1,
      },
      ...sx,
    }}
  >
    {name}
  </Box>
);

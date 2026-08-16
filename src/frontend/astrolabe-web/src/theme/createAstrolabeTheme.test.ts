import { createAstrolabeTheme } from './createAstrolabeTheme';
import { brand, fonts, label, palettes, radii, tintForId, typeScale, weights } from './tokens';

/**
 * Guards the contract between the approved prototype and our Material UI theme.
 *
 * Every expected value here was measured from the prototype's rendered DOM. If a token drifts, this
 * fails rather than the drift shipping unnoticed as "looks a bit off".
 */
describe('createAstrolabeTheme', () => {
  const light = createAstrolabeTheme('light');
  const dark = createAstrolabeTheme('dark');

  describe('colour', () => {
    it.each(['light', 'dark'] as const)('uses the brand primary in %s mode', (scheme) => {
      const theme = createAstrolabeTheme(scheme);

      expect(theme.palette.primary.main).toBe(brand.primary);
      expect(theme.palette.mode).toBe(scheme);
    });

    it('maps the light palette to Material UI slots', () => {
      expect(light.palette.background.default).toBe(palettes.light.background);
      expect(light.palette.background.paper).toBe(palettes.light.surface);
      expect(light.palette.text.primary).toBe(palettes.light.text);
      expect(light.palette.text.secondary).toBe(palettes.light.muted);
    });

    it('maps the dark palette to Material UI slots', () => {
      expect(dark.palette.background.default).toBe(palettes.dark.background);
      expect(dark.palette.background.paper).toBe(palettes.dark.surface);
      expect(dark.palette.text.primary).toBe(palettes.dark.text);
    });

    it('keeps the primary identical across schemes, as the prototype does', () => {
      expect(light.palette.primary.main).toBe(dark.palette.primary.main);
    });
  });

  describe('typography', () => {
    it('uses the prototype dense scale rather than the Material UI default', () => {
      // The prototype's body text is 13px. Material UI ships 16px, which is the single biggest
      // reason an untuned theme does not look like the mockup.
      expect(light.typography.fontSize).toBe(typeScale.body);
      expect(light.typography.body1.fontSize).toBe('0.8125rem');
    });

    it('keeps every heading within the prototype range', () => {
      // Material UI's default h1 is 96px; nothing in the prototype exceeds 26px.
      const headings = [
        light.typography.h1,
        light.typography.h2,
        light.typography.h3,
        light.typography.h4,
        light.typography.h5,
      ];

      for (const heading of headings) {
        const px = Number(String(heading.fontSize).replace('rem', '')) * 16;
        expect(px).toBeLessThanOrEqual(typeScale.display);
        expect(px).toBeGreaterThanOrEqual(typeScale.lead);
      }
    });

    it('sets Playfair Display on display headings and Plus Jakarta Sans on body text', () => {
      expect(light.typography.h1.fontFamily).toContain('Playfair Display');
      expect(light.typography.h3.fontFamily).toContain('Playfair Display');
      expect(light.typography.fontFamily).toContain('Plus Jakarta Sans');
    });

    it('makes semibold the norm, as the prototype does', () => {
      // Weight 600 accounts for nearly every styled element in the prototype.
      expect(light.typography.h3.fontWeight).toBe(weights.semibold);
      expect(light.typography.subtitle2.fontWeight).toBe(weights.semibold);
      expect(light.typography.button.fontWeight).toBe(weights.semibold);
    });

    it('carries the uppercase micro-label signature on overline', () => {
      // Section headers, table headers and status chips all use it.
      expect(light.typography.overline.textTransform).toBe('uppercase');
      expect(light.typography.overline.letterSpacing).toBe(label.tracking);
      expect(light.typography.overline.fontSize).toBe('0.6875rem');
    });

    it('does not shout button labels', () => {
      // The prototype uses sentence case on buttons, and uppercase only on micro-labels.
      expect(light.typography.button.textTransform).toBe('none');
    });

    it('expresses sizes in rem so a reader can scale the interface', () => {
      for (const variant of ['body1', 'body2', 'caption', 'overline', 'h1'] as const) {
        expect(String(light.typography[variant].fontSize)).toMatch(/rem$/);
      }
    });
  });

  describe('shape', () => {
    it('uses the prototype panel radius as the base', () => {
      expect(light.shape.borderRadius).toBe(radii.panel);
    });

    it('keeps chips as pills', () => {
      const chip = light.components?.MuiChip?.styleOverrides?.root as { borderRadius?: number };

      expect(chip?.borderRadius).toBe(radii.pill);
    });
  });

  describe('icons', () => {
    it('declares the Material Symbols variable font axes', () => {
      // The prototype uses Material Symbols Outlined, not the filled Material Icons set. Without
      // the variation axes the glyphs render at the wrong weight and optical size.
      const baseline = light.components?.MuiCssBaseline?.styleOverrides as Record<string, unknown>;
      const symbol = baseline?.['.material-symbols-outlined'] as Record<string, string>;

      expect(symbol.fontFamily).toBe(fonts.icons);
      expect(symbol.fontVariationSettings).toContain("'FILL' 0");
      expect(symbol.fontVariationSettings).toContain("'wght' 400");
    });
  });
});

describe('tintForId', () => {
  it('is deterministic for the same identifier', () => {
    expect(tintForId(3)).toBe(tintForId('3'));
  });

  it('assigns the first tint to the first book, matching the prototype', () => {
    expect(tintForId(1)).toBe('#0E5A6E');
  });

  it('wraps around once the palette is exhausted', () => {
    expect(tintForId(9)).toBe(tintForId(1));
  });
});

import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { brand, radii } from './tokens';
import { createAstrolabeTheme } from './createAstrolabeTheme';

/** Calls the MuiButton root override, which is a function of the button's own props. */
const buttonStyle = (ownerState: { variant: string; color: string }) => {
  const root = createAstrolabeTheme('light').components?.MuiButton?.styleOverrides?.root;

  return (root as (arg: { ownerState: unknown }) => Record<string, unknown>)({ ownerState });
};

const SOURCE_ROOT = join(__dirname, '..');

const sourceFiles = (directory: string): string[] =>
  readdirSync(directory).flatMap((entry) => {
    const path = join(directory, entry);

    if (statSync(path).isDirectory()) {
      return sourceFiles(path);
    }

    return /\.tsx?$/.test(entry) && !/\.test\.tsx?$/.test(entry) ? [path] : [];
  });

/**
 * The corner radii, and the trap underneath them.
 *
 * <p>
 * A bare number in `sx` is <b>not</b> pixels. MUI multiplies `borderRadius` by
 * `theme.shape.borderRadius`, which here is twelve — so `borderRadius: 3`, written while thinking in
 * spacing units, renders at thirty-six pixels. It is silent, it type-checks, and it looks like a
 * deliberate design choice until somebody compares it against the mockup.
 * </p>
 * <p>
 * That is exactly how six panels ended up rounded to 36px and one modal to 42px. The sweep below is
 * the only thing that makes the rule enforceable rather than remembered.
 * </p>
 */
describe('corner radii', () => {
  it('draws cards at the prototype’s 12px, not the modal radius', () => {
    const theme = createAstrolabeTheme('light');
    const outlined = theme.components?.MuiPaper?.styleOverrides?.outlined as
      | { borderRadius?: number }
      | undefined;

    // `border-radius:12px` is the prototype's most common container value by a wide margin. Cards
    // were being drawn at 16, which is what a modal uses, and it showed on every screen at once.
    expect(outlined?.borderRadius).toBe(radii.panel);
    expect(radii.panel).toBe(12);
  });

  it('rounds every button to a pill, whatever its height', () => {
    // The override is a function of ownerState, so it has to be called rather than read.
    expect(buttonStyle({ variant: 'contained', color: 'primary' }).borderRadius).toBe(radii.round);
    expect(buttonStyle({ variant: 'outlined', color: 'primary' }).borderRadius).toBe(radii.round);
  });

  it('keeps a secondary button neutral rather than painting it in the brand colour', () => {
    // Counted from the prototype's markup: 54 outlined buttons with a neutral border and inherited
    // text, 30 filled in #0E5A6E, and not one outlined in the brand colour. MUI's default is the
    // opposite — an outlined button inherits `color="primary"` — which turned every Cancel,
    // Details and Reload on screen teal.
    const outlined = buttonStyle({ variant: 'outlined', color: 'primary' });

    expect(outlined.color).toBeDefined();
    expect(outlined.color).not.toBe(brand.primary);
    expect(outlined.borderColor).not.toBe(brand.primary);
  });

  it('leaves the filled button in the brand colour', () => {
    // The neutral rule is for secondary buttons only. A contained button is the one primary action
    // on a surface and must keep the teal.
    const contained = buttonStyle({ variant: 'contained', color: 'primary' });

    expect(contained.color).toBeUndefined();
    expect(contained.borderColor).toBeUndefined();
  });

  it('uses no bare numeric borderRadius anywhere in the source', () => {
    const offenders = sourceFiles(SOURCE_ROOT).flatMap((path) => {
      const lines = readFileSync(path, 'utf8').split('\n');

      return lines.flatMap((line, index) => {
        const trimmed = line.trim();

        // Prose describing the rule is not a breach of it — this sweep caught the very comment
        // that explains why it exists.
        if (trimmed.startsWith('*') || trimmed.startsWith('//') || trimmed.startsWith('/*')) {
          return [];
        }

        // A quoted value is fine — `borderRadius: '12px'` means twelve pixels and always will.
        return /borderRadius:\s*[0-9]/.test(trimmed)
          ? [`${path.replace(SOURCE_ROOT, 'src')}:${index + 1}  ${trimmed}`]
          : [];
      });
    });

    expect(offenders).toEqual([]);
  });
});

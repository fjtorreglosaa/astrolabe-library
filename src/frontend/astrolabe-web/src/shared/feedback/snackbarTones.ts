import type { SnackbarTone } from './snackbarStore';

/**
 * The three snackbar looks, transcribed from the prototype's `snack(msg, kind)`.
 *
 * <p>
 * Colours are literal rather than palette roles because the prototype's neutral is a dark slate,
 * not the blue that `severity="info"` would give. Two of the three do line up with MUI's semantics;
 * taking two from the theme and one from here would leave the set looking assembled rather than
 * designed.
 * </p>
 * <p>
 * These are fixed in both themes on purpose. A snackbar is an overlay on top of whatever is
 * underneath it, and a surface that changed shade with the theme would have to re-earn its contrast
 * against every background it can appear over.
 * </p>
 */
export interface SnackbarToneStyle {
  background: string;
  icon: string;
}

export const SNACKBAR_TONES: Record<SnackbarTone, SnackbarToneStyle> = {
  error: { background: '#B3261E', icon: 'error' },
  success: { background: '#0F7A63', icon: 'check_circle' },
  info: { background: '#10262E', icon: 'info' },
  // Not in the prototype, which has three. Added for a warning that is neither a failure nor good
  // news — a code that expired, say — and taken from the same family so it does not read as a
  // fourth design.
  warning: { background: '#8A6A28', icon: 'warning' },
};

/** How long a message stays. The prototype's own five seconds. */
export const SNACKBAR_DURATION_MS = 5_000;

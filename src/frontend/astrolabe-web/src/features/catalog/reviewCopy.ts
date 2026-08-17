/**
 * Every string in the rating dialog, transcribed from the prototype.
 */

/** Indexed by star count, so index 0 is the prompt shown before anything is chosen. */
export const RATING_LABELS = [
  'Tap a star to rate',
  'Not for me',
  'It was fine',
  'Good read',
  'Really good',
  'Loved it',
] as const;

export const COMMENT_LIMIT = 500;

export const COMMENT_PLACEHOLDER =
  'What stayed with you? Anything another member should know before borrowing it?';

export const COMMENT_NOTE =
  'Optional. Your name and initials appear next to it in the catalog.';

export const RATING_REQUIRED = 'Pick a star rating before you publish.';

/**
 * The dialog's opening line.
 *
 * <p>
 * It names the date the copy came back, which is also the rule: a member may review a book once they
 * have borrowed it and given it back. Saying so in the first sentence means the restriction never
 * has to be explained as an error — by the time anybody sees this dialog, they have already met it.
 * </p>
 */
export const returnedIntro = (returnedOn: string): string =>
  `You returned this copy on ${returnedOn}. Your rating helps other members and improves your recommendations.`;

export const publishedMessage = (title: string): string =>
  `Thanks — your review of “${title}” is published.`;

export const UPDATED_MESSAGE = 'Your review was updated.';
export const REMOVED_MESSAGE = 'Your review was removed.';

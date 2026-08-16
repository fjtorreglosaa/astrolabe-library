import type { Membership } from '../membership/api/membershipApi';
import type { BookRejection, CopyAvailability, CopyRejection, Genre } from './api/catalogApi';

/**
 * Every member-facing string about access, transcribed from the prototype.
 *
 * The API sends the reason as an enumeration, never as a sentence, so the wording lives in exactly
 * one place. That is the whole point: the same refusal appears on a card, in the detail panel and in
 * a toast, and three hand-written copies would drift on the first edit.
 */

export const GENRE_LABEL: Record<Genre, string> = {
  Fiction: 'Fiction',
  Essay: 'Essay',
  ScienceFiction: 'Science fiction',
  History: 'History',
  Biography: 'Biography',
  Technical: 'Technical',
};

/** The order the prototype shows the filter chips in, with "All" first. */
export const GENRE_FILTERS: readonly (Genre | 'All')[] = [
  'All',
  'Fiction',
  'Essay',
  'ScienceFiction',
  'History',
  'Biography',
  'Technical',
];

/**
 * The badge on a book card. The city is woven in where the prototype weaves it in, which is why this
 * needs the membership rather than the reason alone.
 */
export const bookBadgeLabel = (
  badge: BookRejection,
  membership: Membership | undefined,
): string => {
  switch (badge) {
    case 'AllCopiesOut':
      return 'All copies out';
    case 'NotInBasicPlan':
      return 'Not in Basic plan';
    case 'HomeLibraryOnly':
      return 'Home library only';
    case 'NotInCity':
      return membership?.cityName ? `Not in ${membership.cityName}` : 'Not in your city';
    case 'Unavailable':
      return 'Unavailable';
  }
};

/** The reason beside one branch in the detail panel. */
export const copyReasonLabel = (
  reason: CopyRejection,
  copy: CopyAvailability,
  membership: Membership | undefined,
): string => {
  switch (reason) {
    case 'OutOfStock':
      return 'All copies out';
    case 'NotInBasicCatalog':
      return 'Not in Basic catalog';
    case 'HomeLibraryOnly':
      return membership?.homeLibraryName
        ? `Basic borrows at ${membership.homeLibraryName} only`
        : 'Basic borrows at your home library only';
    case 'OutsideCity':
      return `Outside ${copy.cityName}`;
  }
};

/** The prototype's availability line under a card. */
export const availabilityLabel = (availableCount: number): string =>
  availableCount === 0 ? 'No copies left' : `${availableCount} available`;

/**
 * The tint a book without a cover is drawn with. Chosen from the identifier so the same book always
 * looks the same, which is what BR-CAT-005 requires — a random tint would make a book unrecognisable
 * between two visits.
 */
const TINTS = [
  '#0E5A6E',
  '#4A6B7C',
  '#7A5C4A',
  '#5C6B4A',
  '#6B4A5C',
  '#3F5E62',
] as const;

export const tintFor = (bookId: string): string => {
  // Summed over the whole identifier rather than a single character, so books created together do
  // not all land on the same colour.
  const sum = [...bookId].reduce((total, character) => total + character.charCodeAt(0), 0);

  return TINTS[sum % TINTS.length];
};

/** Two initials, as the prototype draws a cover placeholder. */
export const coverInitials = (title: string): string =>
  [...title.split(' ').filter(Boolean).slice(0, 2)]
    .map((word) => word[0]?.toUpperCase() ?? '')
    .join('');

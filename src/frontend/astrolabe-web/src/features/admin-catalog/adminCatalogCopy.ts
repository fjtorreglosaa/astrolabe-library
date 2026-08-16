import type { BookStatus, Genre, RemovalReason, RepairReason } from './api/adminCatalogApi';

/**
 * Book management wording.
 *
 * The reasons are the part that matters. `BR-CAT-025` wants an audit note on every lifecycle
 * change, and a typed reason is what makes the trail answerable later — "why did we lose forty
 * copies last quarter" has an answer when the reasons are a closed set, and none when they are free
 * text somebody typed differently each time.
 */

/** The three steps, transcribed from the prototype's `WIZ_STEPS`. */
export const WIZARD_STEPS = ['Book details', 'Copies & pricing', 'Review'] as const;

export const GENRE_LABEL: Record<Genre, string> = {
  Fiction: 'Fiction',
  Essay: 'Essay',
  History: 'History',
  Biography: 'Biography',
  ScienceFiction: 'Science fiction',
  Technical: 'Technical',
};

export const STATUS_LABEL: Record<BookStatus, string> = {
  Draft: 'Draft',
  Catalog: 'In catalogue',
  Repair: 'In repair',
  Deleted: 'Removed',
};

export const STATUS_COLOR: Record<BookStatus, 'default' | 'success' | 'warning' | 'error'> = {
  Draft: 'default',
  Catalog: 'success',
  Repair: 'warning',
  Deleted: 'error',
};

export const STATUS_ICON: Record<BookStatus, string> = {
  Draft: 'edit_note',
  Catalog: 'check_circle',
  Repair: 'build',
  Deleted: 'delete',
};

export const REPAIR_REASON_LABEL: Record<RepairReason, string> = {
  DamagedSpine: 'Damaged spine',
  WaterDamage: 'Water damage',
  MissingPages: 'Missing pages',
  Rebinding: 'Rebinding',
  CoverReplacement: 'Cover replacement',
  Other: 'Other',
};

export const REMOVAL_REASON_LABEL: Record<RemovalReason, string> = {
  Donated: 'Donated',
  DamagedBeyondRepair: 'Damaged beyond repair',
  LostByMember: 'Lost by a member',
  WithdrawnFromCollection: 'Withdrawn from the collection',
  Other: 'Other',
};

export const STATUS_FILTERS: readonly (BookStatus | 'All')[] = [
  'All',
  'Draft',
  'Catalog',
  'Repair',
  'Deleted',
];

/** A draft is invisible to members until published, and that is worth saying out loud. */
export const DRAFT_NOTE =
  'Saved as a draft. Members cannot see it until you publish it, and you can pick it up any time.';

export const PUBLISH_NOTE = 'Publishing puts this book in front of members straight away.';

/** Closing the wizard with unsaved work. The prototype guards this too. */
export const DISCARD_TITLE = 'Discard this book?';
export const DISCARD_BODY =
  'Nothing here has been saved. Save it as a draft instead and you can finish it later.';

// ---------- Cover images, BR-CAT-005 ----------

/** The prototype's own limit. Mirrored server-side, which is the one that counts. */
export const MAX_COVER_BYTES = 4 * 1024 * 1024;

const ALLOWED_COVER_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

export const isAllowedCoverType = (contentType: string): boolean =>
  ALLOWED_COVER_TYPES.includes(contentType.toLowerCase());

/** The prototype's wording, kept word for word. */
export const COVER_NOT_AN_IMAGE = 'That file is not an image. Use JPG, PNG or WebP.';
export const COVER_TOO_LARGE = 'That image is larger than 4 MB. Pick a smaller file.';

export const COVER_HINT =
  'JPG, PNG or WebP up to 4 MB. Portrait 3:4 looks best — the image is cropped to fit each card.';

/**
 * Said when no image is chosen. Phrased as a statement rather than an offer because BR-CAT-005 makes
 * the colour a consequence of the book's identity, not something anybody picks.
 */
export const NO_IMAGE_NOTE =
  'No image? The book keeps the colour shown, generated from its own identity so it always looks the same.';

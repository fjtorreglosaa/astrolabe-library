import type { Membership, PlanChangeLoss, PlanChangeQuote, PlanTier, ReachKind } from './api/membershipApi';

/**
 * Every member-facing string about plans, transcribed from the prototype.
 *
 * Kept in one module rather than inline in the components: the same sentences appear in the plan
 * cards, the change modal and its confirmation step, and three copies would drift the first time one
 * of them was edited.
 */

/** Cents to the prototype's money format. */
export const money = (cents: number): string => `$${(cents / 100).toFixed(2)}`;

/**
 * The prototype writes dates as "Sep 16, 2026".
 *
 * Rendered in UTC, not in the viewer's zone. A renewal date is a billing fact the API computes in
 * UTC: formatting a UTC midnight locally shows the previous day to everyone west of Greenwich, so a
 * member would read "Sep 15" while the system charges on the 16th.
 */
export const formatDate = (iso: string): string =>
  new Date(iso).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    timeZone: 'UTC',
  });

export const PLAN_SUMMARY: Record<PlanTier, string> = {
  Basic: 'One library, Basic catalog',
  Plus: 'Your whole city, full catalog',
  Max: 'Every library, points on purchases',
};

export const PLAN_BULLETS: Record<PlanTier, readonly string[]> = {
  Basic: [
    'Borrowing at 1 library of your choice',
    'Titles included in the Basic catalog',
    'No purchase discounts',
  ],
  Plus: [
    'Borrowing at every library in your city',
    'Full catalog with no restrictions',
    'Purchase discounts within your city',
  ],
  Max: [
    'Borrowing at every library on the platform',
    'Purchase discounts in every city',
    'Points on every purchase',
  ],
};

export const REACH_LABEL: Record<ReachKind, string> = {
  HomeLibraryOnly: 'Your home library',
  City: 'Every library in your city',
  Network: 'Every library on the platform',
};

/**
 * The prototype's own wording for each disclosure. The city is woven in where the prototype weaves
 * it in, which is why this takes the membership rather than the loss alone.
 */
export const lossSentence = (loss: PlanChangeLoss, membership: Membership): string => {
  switch (loss) {
    case 'RewardPoints':
      return 'Reward points stop accruing and cannot be redeemed after the change.';
    case 'HomeLibraryAndBasicCatalog':
      return membership.cityName
        ? `Borrowing limited to ${membership.cityName} — your home library and Basic-catalog titles.`
        : 'Borrowing limited to your home library and Basic-catalog titles.';
    case 'Recommendations':
      return 'AI recommendations turn off.';
  }
};

/** The status line under the Membership heading. */
export const planStatusLine = (membership: Membership): string =>
  membership.priceCents === 0
    ? 'Basic is free, so there is nothing to renew.'
    : `Active until ${formatDate(membership.renewsOn)} · renews automatically at ${money(membership.priceCents)}`;

/** The line shown while a downgrade is waiting for the renewal date. */
export const pendingChangeLine = (membership: Membership): string | null => {
  if (!membership.scheduledChange) {
    return null;
  }

  const on = formatDate(membership.scheduledChange.effectiveOn);

  return `You keep ${membership.plan} until ${on}. ${membership.scheduledChange.target} starts that day and nothing is charged before then.`;
};

export interface QuoteCopy {
  kicker: string;
  title: string;
  sub: string;
  rows: { label: string; value: string }[];
  dueLabel: string;
  due: string;
  after: string;
  cta: string;
  confirmTitle: string;
  confirmBody: string;
}

/**
 * The modal's copy for a given quote. A single function rather than a component so the wording can
 * be asserted in tests without rendering.
 */
export const quoteCopy = (quote: PlanChangeQuote, membership: Membership): QuoteCopy => {
  const on = formatDate(quote.effectiveOn);
  const renews = formatDate(membership.renewsOn);
  const targetPrice = PLAN_PRICE_CENTS[quote.to];

  if (quote.direction === 'upgrade') {
    return {
      kicker: 'Upgrade',
      title: `Move up to ${quote.to}`,
      sub: `Your ${quote.from} month runs to ${renews}. You only pay the difference for the ${membership.daysRemaining} days left, never twice for the same period.`,
      rows: [
        {
          label: `${quote.to} for ${membership.daysRemaining} remaining days`,
          value: money(quote.chargeCents),
        },
        {
          label: `Credit for the ${quote.from} days you already paid`,
          value: `−${money(quote.creditCents)}`,
        },
      ],
      dueLabel: 'Due today',
      due: money(quote.amountDueCents),
      after: `From ${renews} you pay ${money(targetPrice)} every month.`,
      cta: `Pay ${money(quote.amountDueCents)} and upgrade`,
      confirmTitle: `Charge ${money(quote.amountDueCents)} now?`,
      confirmBody: `We charge ${money(quote.amountDueCents)} to your card and ${quote.to} benefits apply immediately.`,
    };
  }

  return {
    kicker: 'Scheduled downgrade',
    title: `Move down to ${quote.to}`,
    sub: 'Downgrades wait for the end of the period you already paid. Nothing is charged now and nothing is refunded.',
    rows: [
      { label: `${quote.from} stays active until`, value: on },
      { label: `${quote.to} starts on`, value: on },
    ],
    dueLabel: 'Charged today',
    due: money(0),
    after:
      targetPrice > 0
        ? `From ${on} you pay ${money(targetPrice)} every month.`
        : `From ${on} you pay nothing. Borrowing narrows to your home library and the Basic catalog.`,
    cta: 'Schedule the change',
    confirmTitle: `Schedule ${quote.to} for ${on}?`,
    confirmBody: `You keep ${quote.from} until ${on}. On that date the plan becomes ${quote.to}. You can cancel the change any time before then.`,
  };
};

/**
 * Prices, for the sentences that name a plan the member is not on and whose price is therefore not
 * in the membership payload. The plan list from the API remains the authority for what is charged.
 */
export const PLAN_PRICE_CENTS: Record<PlanTier, number> = {
  Basic: 0,
  Plus: 699,
  Max: 1299,
};

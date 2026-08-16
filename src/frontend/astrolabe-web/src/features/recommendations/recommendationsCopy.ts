/**
 * Recommendation wording.
 *
 * The explanatory notes come from the **server**, not from here: which sentence is right depends on
 * whether a library is connected, and the browser is never told that. What lives here is the copy
 * that does not depend on a rule.
 */

/** BR-REC-002, for the member the surface is closed to. */
export const PLAN_NOTE =
  'Personalised picks are part of the Plus and Max plans. Basic keeps full browsing and reservations at your home library, without model-generated suggestions.';

export const REFRESH_LIMIT_NOTE =
  'These were refreshed a moment ago. Recommendations cost your library money to generate, so there is a short wait between refreshes.';

/** The staff panel's own introduction, transcribed from the prototype. */
export const CONFIG_INTRO =
  'Each library runs on its own key. Members of a connected library get model-generated picks; everywhere else they see the most-borrowed fallback. Plus and Max members only — Basic never sees this surface.';

/**
 * Said beside the key field. A staff member typing a secret into a box deserves to know where it
 * goes, and "we never show it back" is the part that matters when they wonder later.
 */
export const KEY_PRIVACY_NOTE =
  'Stored encrypted. It is never shown again, by this screen or any other — if you lose it, paste a new one.';

export const SOURCE_LABEL: Record<'Model' | 'Fallback', string> = {
  Model: 'Personalised for you',
  Fallback: 'Most borrowed in your genres',
};

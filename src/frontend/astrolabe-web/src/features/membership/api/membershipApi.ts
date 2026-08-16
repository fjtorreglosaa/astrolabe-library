import { httpClient } from '../../../shared/api/httpClient';

/** The three member tiers, as the API names them. */
export type PlanTier = 'Basic' | 'Plus' | 'Max';

/** Where a plan lets a member borrow. */
export type ReachKind = 'HomeLibraryOnly' | 'City' | 'Network';

/** Which way a change moves. Decided by the API from plan rank, never inferred here. */
export type ChangeDirection = 'upgrade' | 'downgrade';

/**
 * What a downgrade costs the member in entitlements.
 *
 * The API sends the reason, not the sentence, so the wording lives in exactly one place. Mapping it
 * here rather than server-side keeps the copy beside every other string the screen renders.
 */
export type PlanChangeLoss =
  | 'RewardPoints'
  | 'HomeLibraryAndBasicCatalog'
  | 'Recommendations';

export interface ScheduledPlanChange {
  target: PlanTier;
  effectiveOn: string;
  requestedAt: string;
}

export interface Membership {
  plan: PlanTier;
  reach: ReachKind;
  priceCents: number;
  discountPercent: number;
  earnsPoints: boolean;
  seesRecommendations: boolean;
  cycleStartedOn: string;
  renewsOn: string;
  daysRemaining: number;
  cityId: string | null;
  cityName: string | null;
  homeLibraryId: string | null;
  homeLibraryName: string | null;
  scheduledChange: ScheduledPlanChange | null;
  canChangeCityThisCycle: boolean;
}

export interface PlanOption {
  plan: PlanTier;
  priceCents: number;
  reach: ReachKind;
  discountPercent: number;
  earnsPoints: boolean;
  seesRecommendations: boolean;
  isCurrent: boolean;
  direction: ChangeDirection | null;
}

export interface PlanChangeQuote {
  from: PlanTier;
  to: PlanTier;
  direction: ChangeDirection;
  chargeCents: number;
  creditCents: number;
  amountDueCents: number;
  effectiveOn: string;
  whatYouLose: PlanChangeLoss[];
}

export interface PlanChangeResult {
  plan: PlanTier;
  appliedImmediately: boolean;
  amountChargedCents: number;
  effectiveOn: string;
}

export const getMyMembership = async (): Promise<Membership> => {
  const { data } = await httpClient.get<Membership>('/api/v1/membership/me');
  return data;
};

export const getPlans = async (): Promise<PlanOption[]> => {
  const { data } = await httpClient.get<PlanOption[]>('/api/v1/membership/plans');
  return data;
};

export const quotePlanChange = async (target: PlanTier): Promise<PlanChangeQuote> => {
  const { data } = await httpClient.get<PlanChangeQuote>(
    `/api/v1/membership/plans/${target}/quote`,
  );
  return data;
};

export const changePlan = async (target: PlanTier): Promise<PlanChangeResult> => {
  const { data } = await httpClient.post<PlanChangeResult>('/api/v1/membership/plan', {
    targetPlan: target,
  });
  return data;
};

export const cancelScheduledPlanChange = async (): Promise<void> => {
  await httpClient.delete('/api/v1/membership/plan/scheduled-change');
};

export const changeResidence = async (countryId: string, cityId: string): Promise<void> => {
  await httpClient.put('/api/v1/membership/residence', { countryId, cityId });
};

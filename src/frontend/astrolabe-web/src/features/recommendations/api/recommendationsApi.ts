import { httpClient } from '../../../shared/api/httpClient';

/** The two vendors the prototype offers. Named as it names them: "Claude", never "Anthropic". */
export type AiProvider = 'Claude' | 'OpenAI';

/** Where a set came from. The screen says which rather than passing a fallback off as a pick. */
export type RecommendationSource = 'Model' | 'Fallback';

export interface Recommendation {
  bookId: string;
  title: string;
  author: string;
  coverUrl: string | null;
  /** Always present — BR-REC-010. A suggestion without one never reaches here. */
  reason: string;
  matchPercent: number;
}

export interface RecommendationSet {
  source: RecommendationSource;
  /** The sentence explaining this answer, chosen server-side because the client cannot see why. */
  note: string;
  generatedAt: string;
  canRegenerate: boolean;
  items: Recommendation[];
}

/**
 * One library's configuration, for its staff.
 *
 * There is **no credential field here in any form** — not masked, not truncated, not a length.
 * BR-REC-004, and the server's DTO has nowhere to put one either.
 */
export interface LibraryAiStatus {
  libraryId: string;
  libraryName: string;
  provider: AiProvider | null;
  isConnected: boolean;
  isEnabled: boolean;
  isVerified: boolean;
  lastVerifiedAt: string | null;
  /** "Claude connected" or "Not configured" — the prototype's own words. */
  status: string;
  note: string;
}

export const getMyRecommendations = async (): Promise<RecommendationSet> => {
  const { data } = await httpClient.get<RecommendationSet>('/api/v1/recommendations');
  return data;
};

/** A POST because it spends a library's money. BR-REC-011 rate limits it server-side. */
export const refreshRecommendations = async (): Promise<RecommendationSet> => {
  const { data } = await httpClient.post<RecommendationSet>('/api/v1/recommendations/refresh');
  return data;
};

export const getLibraryAiStatus = async (): Promise<LibraryAiStatus[]> => {
  const { data } = await httpClient.get<LibraryAiStatus[]>(
    '/api/v1/admin/recommendations/libraries',
  );
  return data;
};

/**
 * The prototype's "Save and test". The key travels in one direction only and is never read back —
 * there is no endpoint that would return it.
 */
export const configureLibraryAi = async (
  libraryId: string,
  provider: AiProvider,
  credential: string,
): Promise<LibraryAiStatus> => {
  const { data } = await httpClient.put<LibraryAiStatus>(
    `/api/v1/admin/recommendations/libraries/${libraryId}`,
    { provider, credential },
  );
  return data;
};

/** Switches off and keeps the stored key — BR-REC-012. */
export const disableLibraryAi = async (libraryId: string): Promise<void> => {
  await httpClient.delete(`/api/v1/admin/recommendations/libraries/${libraryId}`);
};

import { httpClient } from '../../../shared/api/httpClient';
import type { Paged } from '../../catalog/api/catalogApi';

/** The prototype's three, and no others. */
export type TicketStatus = 'Created' | 'InReview' | 'Resolved';

/** The prototype's five categories, verbatim. */
export type TicketCategory =
  | 'PaymentsAndFines'
  | 'ReservationsAndReturns'
  | 'CatalogueAndAvailability'
  | 'AccountAndPlan'
  | 'SomethingIsBroken';

export type TicketAuthor = 'Member' | 'Agent';

/** Assign, resolve or reopen. One route, three verbs — the server shares every guard. */
export type TicketTransition = 'Assign' | 'Resolve' | 'Reopen';

export interface TicketMessage {
  id: string;
  author: TicketAuthor;
  authorName: string;
  text: string;
  writtenAt: string;
}

export interface TicketSummary {
  id: string;
  /** `TCK-NNNN`. What a member quotes on the phone. */
  reference: string;
  subject: string;
  category: string;
  status: string;
  agentName: string | null;
  libraryName: string;
  memberName: string;
  rating: number | null;
  createdAt: string;
  updatedAt: string;
}

export interface Ticket extends TicketSummary {
  review: string | null;
  /**
   * Both decided server-side. They depend on the ticket's status and on who is asking, and working
   * them out here would be a second copy of BR-SUP-005 and BR-SUP-011.
   */
  canReply: boolean;
  canRate: boolean;
  messages: TicketMessage[];
}

export const searchTickets = async (search: {
  term?: string;
  status?: TicketStatus;
  page?: number;
  pageSize?: number;
}): Promise<Paged<TicketSummary>> => {
  const { data } = await httpClient.get<Paged<TicketSummary>>('/api/v1/support/tickets', {
    params: {
      term: search.term || undefined,
      status: search.status,
      page: search.page ?? 1,
      pageSize: search.pageSize ?? 20,
    },
  });
  return data;
};

export const getTicket = async (ticketId: string): Promise<Ticket> => {
  const { data } = await httpClient.get<Ticket>(`/api/v1/support/tickets/${ticketId}`);
  return data;
};

export const openTicket = async (input: {
  subject: string;
  body: string;
  category: TicketCategory;
  libraryId: string;
}): Promise<Ticket> => {
  const { data } = await httpClient.post<Ticket>('/api/v1/support/tickets', input);
  return data;
};

/** No author field: the server decides from the caller's role, so a member cannot post as staff. */
export const replyToTicket = async (ticketId: string, text: string): Promise<void> => {
  await httpClient.post(`/api/v1/support/tickets/${ticketId}/messages`, { text });
};

export const transitionTicket = async (
  ticketId: string,
  transition: TicketTransition,
): Promise<void> => {
  await httpClient.post(`/api/v1/support/tickets/${ticketId}/${transition}`);
};

export const rateTicket = async (
  ticketId: string,
  stars: number,
  review: string | null,
): Promise<void> => {
  await httpClient.post(`/api/v1/support/tickets/${ticketId}/rating`, { stars, review });
};

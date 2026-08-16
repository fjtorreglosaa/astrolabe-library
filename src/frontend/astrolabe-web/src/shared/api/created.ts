/**
 * The body every create answers when it has only an identifier to give back.
 *
 * There used to be three shapes — a bare `Guid`, an `{ id }` and an `{ invitationId }` — and this
 * client read `.id` from all of them, silently getting `undefined` from two. `GLOBAL-022` settled
 * it on one, and this type exists so the next screen cannot get it wrong either.
 */
export interface CreatedResource {
  id: string;
}

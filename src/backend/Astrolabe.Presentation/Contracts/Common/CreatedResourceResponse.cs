namespace Astrolabe.Presentation.Contracts.Common;

/// <summary>
/// The body every create answers when it has only an identifier to give back.
///
/// <para>
/// One shape, because there used to be three (`GLOBAL-022`): a bare <c>Guid</c>, an anonymous
/// <c>{ id }</c>, and an <c>{ invitationId }</c>. That is not merely untidy — the first client
/// written against them read <c>.id</c> from all three and silently received <c>undefined</c> from
/// two, which surfaced only when the screen was exercised against a running server.
/// </para>
/// <para>
/// A record rather than an anonymous object so it appears in the generated schema, and an object
/// rather than a bare identifier so a field can be added later without breaking every caller.
/// Creates that have a whole resource to return still return it — <c>PlaceOrder</c> answers an
/// order, not an identifier — and that is a different thing, not an inconsistency.
/// </para>
/// </summary>
public sealed record CreatedResourceResponse(Guid Id);

namespace Astrolabe.Presentation.Contracts.Billing;

/// <summary>The body of a card payment. The member comes from the token, never from the payload.</summary>
public sealed record PayFinesRequest(IReadOnlyList<Guid> FineIds, Guid PaymentMethodId);

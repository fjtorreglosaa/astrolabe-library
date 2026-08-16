namespace Astrolabe.Presentation.Contracts.Billing;

/// <summary>The body of a desk code request. It settles nothing.</summary>
public sealed record IssueDeskPaymentRequest(IReadOnlyList<Guid> FineIds);

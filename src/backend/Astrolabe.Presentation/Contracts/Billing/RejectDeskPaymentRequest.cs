namespace Astrolabe.Presentation.Contracts.Billing;

/// <summary>The body of a rejection. The reason is required by BR-BIL-019.</summary>
public sealed record RejectDeskPaymentRequest(string Reason);

using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Billing;

namespace Astrolabe.Application.Features.Billing.Queries.GetMyPaymentMethods;

/// <summary>The caller's cards, as far as this system knows them.</summary>
public sealed record GetMyPaymentMethodsQuery : IQuery<IReadOnlyList<PaymentMethodDto>>;

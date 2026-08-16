namespace Astrolabe.Application.Contracts.Membership;

/// <summary>
/// What a plan change would cost and when it would take effect, shown before the member confirms.
/// BR-MBR-020 requires the losses to be listed too, which is why <c>WhatYouLose</c> is computed here
/// rather than assembled in the frontend.
/// </summary>
public sealed record PlanChangeQuoteDto(
    string From,
    string To,
    string Direction,
    int ChargeCents,
    int CreditCents,
    int AmountDueCents,
    DateTimeOffset EffectiveOn,
    IReadOnlyList<string> WhatYouLose);

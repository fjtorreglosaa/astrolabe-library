using Astrolabe.Domain.Features.Reservations.Enums;

namespace Astrolabe.Presentation.Contracts.Reservations;

/// <summary>The body of a handover: how the copy went back, and the code read out.</summary>
public sealed record BeginReturnRequest(ReturnMethod Method, string Code);

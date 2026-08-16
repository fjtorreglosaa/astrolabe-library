using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Application.Features.Identity.Commands.Register;

/// <summary>
/// Public registration. Implements BR-IDN-001, BR-IDN-002, BR-IDN-003 and BR-IDN-030.
/// </summary>
/// <param name="Plan">
/// The plan the visitor chose on the pricing screen. A <see cref="PlanTier"/> and not a role:
/// registration decides what somebody buys, never what authority they hold.
/// </param>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    Guid CountryId,
    Guid CityId,
    PlanTier Plan) : ICommand;

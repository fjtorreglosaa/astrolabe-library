using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Application.Features.Identity.Commands.Register;

/// <summary>
/// Public registration. Implements BR-IDN-001, BR-IDN-002, BR-IDN-003 and BR-IDN-030.
/// </summary>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    Guid CountryId,
    Guid CityId,
    UserRole Plan) : ICommand;

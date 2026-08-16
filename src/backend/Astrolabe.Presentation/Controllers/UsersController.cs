using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Application.Features.Identity.Commands.AdministerUser;
using Astrolabe.Application.Features.Identity.Commands.ResendVerificationForUser;
using Astrolabe.Application.Features.Identity.Queries.GetUserDetail;
using Astrolabe.Application.Features.Identity.Queries.SearchUsers;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Primitives;
using Astrolabe.Presentation.Contracts.Identity;
using Astrolabe.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// The staff user directory: who exists, and blocking, restoring, deleting or chasing them.
///
/// <para>
/// Separate from <c>AuthController</c>, which is about the caller's own account and is largely
/// anonymous. This one is staff acting on somebody else's, and every route is behind
/// <c>StaffOnly</c> — but the policy is the outer door only. What an administrator may actually
/// reach is decided inside each handler by BR-NET-006 and BR-NET-010, because a policy cannot see
/// which city an account belongs to.
/// </para>
/// </summary>
[Route("api/v1/users")]
[Authorize(Policy = Policies.StaffOnly)]
public sealed class UsersController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet]
    [ProducesResponseType<PagedResult<UserSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? term,
        [FromQuery] UserStatus? status,
        [FromQuery] UserRole? role,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] UserSortKey sortBy = UserSortKey.CreatedAt,
        [FromQuery] SortDirection direction = SortDirection.Descending,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new SearchUsersQuery(term, status, role, includeDeleted, sortBy, direction, page, pageSize),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetail(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetUserDetailQuery(userId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Block, unblock, delete or restore. One route with an action rather than four verbs: they
    /// share every guard, and splitting them would be four places for one rule to drift.
    /// </summary>
    [HttpPost("{userId:guid}/administer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Administer(
        Guid userId, AdministerUserRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new AdministerUserCommand(userId, request.Action), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("{userId:guid}/resend-verification")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResendVerification(
        Guid userId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ResendVerificationForUserCommand(userId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}

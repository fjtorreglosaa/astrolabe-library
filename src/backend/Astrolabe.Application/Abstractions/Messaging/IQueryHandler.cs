using Astrolabe.Domain.Primitives;
using MediatR;

namespace Astrolabe.Application.Abstractions.Messaging;

/// <summary>
/// Handles a query. Validation runs inside the handler — there are no pipeline behaviors in this
/// solution, per SDD_PLIUS_STRATEGY.md section 9.1 and Rule 4.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;

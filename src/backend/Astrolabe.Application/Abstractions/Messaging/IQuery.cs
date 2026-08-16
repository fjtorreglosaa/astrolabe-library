using Astrolabe.Domain.Primitives;
using MediatR;

namespace Astrolabe.Application.Abstractions.Messaging;

/// <summary>A read operation. Always yields a <see cref="Result{TResponse}"/>.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

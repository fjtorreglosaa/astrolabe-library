using Astrolabe.Domain.Primitives;
using MediatR;

namespace Astrolabe.Application.Abstractions.Messaging;

/// <summary>A write operation. Always yields a <see cref="Result"/> with no value.</summary>
public interface ICommand : IRequest<Result>;

/// <summary>A write operation that yields a value, for example the identifier it created.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

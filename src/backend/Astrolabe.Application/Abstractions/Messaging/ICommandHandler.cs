using Astrolabe.Domain.Primitives;
using MediatR;

namespace Astrolabe.Application.Abstractions.Messaging;

/// <summary>
/// Handles a command. Validation runs inside the handler — there are no pipeline behaviors in this
/// solution, per SDD_PLIUS_STRATEGY.md section 9.1 and Rule 4.
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

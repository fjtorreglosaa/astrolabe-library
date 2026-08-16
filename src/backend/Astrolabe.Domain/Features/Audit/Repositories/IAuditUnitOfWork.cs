using Astrolabe.Domain.Abstractions.Persistence;

namespace Astrolabe.Domain.Features.Audit.Repositories;

/// <summary>
/// The audit trail's unit of work.
///
/// <para>
/// Audit is its own bounded context rather than part of <c>identity</c>, because four domains write
/// to it and none of them owns it. While it lived under identity, a network handler had to inject
/// the whole <c>IIdentityUnitOfWork</c> — users, sessions, tokens and all — to append a single row,
/// which is exactly the coupling a unit of work per context exists to prevent.
/// </para>
/// <para>
/// Every unit of work in the solution shares one <c>DbContext</c>, so a handler that stages an audit
/// entry here and commits through its own unit of work still writes both in one transaction. That
/// matters: BR-CAT-025 and BR-NET-017 require the entry, so it must not be able to go missing while
/// the change it describes succeeds.
/// </para>
/// </summary>
public interface IAuditUnitOfWork : IUnitOfWork
{
    IAuditRepository Entries { get; }
}

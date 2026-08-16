using Astrolabe.Domain.Features.Identity.ValueObjects;

namespace Astrolabe.Application.Abstractions.Identity;

/// <summary>
/// Hashes and verifies passwords. Abstracted so the algorithm can be replaced — for example with
/// Argon2id — without touching a single use case.
/// </summary>
public interface IPasswordHasher
{
    PasswordHash Hash(string password);

    bool Verify(string password, PasswordHash hash);
}

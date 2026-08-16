using System.Reflection;
using Astrolabe.Domain.Primitives;
using FluentAssertions;

namespace Astrolabe.Domain.Tests;

/// <summary>
/// Enforces the dependency rules of SDD_PLIUS_STRATEGY.md section 9.1 and RULE 3 automatically,
/// so a violation fails the build rather than waiting to be caught in review.
/// </summary>
[TestFixture]
public sealed class ArchitectureTests
{
    /// <summary>Assemblies the Domain layer is permitted to depend on: the runtime, and nothing else.</summary>
    private static readonly string[] AllowedPrefixes =
    [
        "System",
        "netstandard",
        "mscorlib"
    ];

    [Test]
    public void Domain_ReferencesNoExternalAssemblies()
    {
        var domain = typeof(Result).Assembly;

        var offenders = domain
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => !AllowedPrefixes.Any(prefix =>
                name.Equals(prefix, StringComparison.Ordinal) ||
                name.StartsWith(prefix + ".", StringComparison.Ordinal)))
            .ToArray();

        offenders.Should().BeEmpty(
            "the Domain layer must have zero external dependencies, but it references: {0}",
            string.Join(", ", offenders));
    }

    [Test]
    public void Domain_DoesNotReferenceAnyOtherSolutionProject()
    {
        var domain = typeof(Result).Assembly;

        domain.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Should().NotContain(name => name.StartsWith("Astrolabe.", StringComparison.Ordinal));
    }

    [Test]
    public void Domain_DoesNotReferenceEntityFrameworkOrAspNetCore()
    {
        // Called out explicitly because these are the two most likely accidental leaks.
        var referenced = typeof(Result).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        referenced.Should().NotContain(name => name.Contains("EntityFrameworkCore", StringComparison.Ordinal));
        referenced.Should().NotContain(name => name.Contains("AspNetCore", StringComparison.Ordinal));
    }

    [Test]
    public void Domain_UsesNoDecimalOrDoubleForMoney()
    {
        // Enforces BR-GLOBAL-001 structurally: Money must expose only integral cents.
        var moneyProperties = typeof(Money)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(decimal)
                     || p.PropertyType == typeof(double)
                     || p.PropertyType == typeof(float))
            .Select(p => p.Name)
            .ToArray();

        moneyProperties.Should().BeEmpty(
            "money must never be represented as a floating point or decimal type");
    }
}

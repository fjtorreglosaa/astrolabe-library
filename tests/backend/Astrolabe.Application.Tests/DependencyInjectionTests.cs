using Astrolabe.Application;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabe.Application.Tests;

/// <summary>
/// Guards the application layer composition. The negative assertions matter as much as the positive
/// one: SDD_PLIUS_STRATEGY.md section 9.1 and RULE 4 forbid pipeline behaviors, so this test fails
/// the build if one is ever registered.
/// </summary>
[TestFixture]
public sealed class DependencyInjectionTests
{
    private static readonly IConfiguration EmptyConfiguration =
        new ConfigurationBuilder().AddInMemoryCollection([]).Build();

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(EmptyConfiguration);
        return services.BuildServiceProvider();
    }

    [Test]
    public void AddApplication_RegistersTheMediatorAsISender()
    {
        using var provider = BuildProvider();

        provider.GetService<ISender>().Should().NotBeNull();
    }

    [Test]
    public void AddApplication_RegistersNoPipelineBehaviors()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(EmptyConfiguration);

        var behaviors = services
            .Where(d => d.ServiceType.IsGenericType
                     && d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToArray();

        behaviors.Should().BeEmpty(
            "validation runs inside handlers and logging uses the built-in tooling; "
            + "pipeline behaviors are forbidden by the methodology");
    }

    [Test]
    public void AddApplication_RegistersNoPreOrPostProcessors()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(EmptyConfiguration);

        var processors = services
            .Where(d => d.ServiceType.IsGenericType
                     && (d.ServiceType.GetGenericTypeDefinition() == typeof(IRequestPreProcessor<>)
                      || d.ServiceType.GetGenericTypeDefinition() == typeof(IRequestPostProcessor<,>)))
            .ToArray();

        processors.Should().BeEmpty(
            "pre and post processors are the same cross-cutting escape hatch as pipeline behaviors");
    }
}

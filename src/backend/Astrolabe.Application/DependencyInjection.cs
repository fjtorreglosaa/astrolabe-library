using System.Reflection;
using Astrolabe.Application.Shared.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabe.Application;

/// <summary>
/// Registers the application layer. Deliberately registers no pipeline behaviors: validation runs
/// inside handlers and logging uses the built-in tooling, per SDD_PLIUS_STRATEGY.md section 9.1.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services
            .AddOptions<MailOptions>()
            .Bind(configuration.GetSection(MailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Stateless: composing an email allocates nothing that must survive a request.
        services.AddSingleton<IdentityMailTemplates>();
        services.AddSingleton<NetworkMailTemplates>();

        return services;
    }
}

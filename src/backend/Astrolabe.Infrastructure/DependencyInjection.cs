using Astrolabe.Application.Abstractions.Mail;
using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Infrastructure.Integrations.Mail;
using Astrolabe.Infrastructure.Features.Identity;
using Astrolabe.Infrastructure.Features.Billing;
using Astrolabe.Infrastructure.Features.Membership;
using Astrolabe.Infrastructure.Features.Network;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Membership.Repositories;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Features.Reservations.Repositories;
using Astrolabe.Domain.Features.Store.Repositories;
using Astrolabe.Infrastructure.Persistence;
using Astrolabe.Infrastructure.Persistence.Repositories;
using Astrolabe.Infrastructure.Persistence.Repositories.Audit;
using Astrolabe.Infrastructure.Persistence.Repositories.Billing;
using Astrolabe.Infrastructure.Persistence.Repositories.Catalog;
using Astrolabe.Infrastructure.Persistence.Repositories.Identity;
using Astrolabe.Infrastructure.Persistence.Repositories.Membership;
using Astrolabe.Infrastructure.Persistence.Repositories.Network;
using Astrolabe.Infrastructure.Persistence.Repositories.Reservations;
using Astrolabe.Infrastructure.Persistence.Repositories.Store;
using Astrolabe.Infrastructure.Persistence.Seeding;
using Astrolabe.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Astrolabe.Application.Abstractions.Recommendations;
using Astrolabe.Domain.Features.Recommendations.Repositories;
using Astrolabe.Infrastructure.Features.Recommendations;
using Astrolabe.Infrastructure.Integrations.Ai;
using Astrolabe.Infrastructure.Persistence.Repositories.Recommendations;
using Astrolabe.Application.Abstractions.Notifications;
using Astrolabe.Domain.Features.Notifications.Repositories;
using Astrolabe.Infrastructure.Features.Notifications;
using Astrolabe.Infrastructure.Persistence.Repositories.Notifications;
using Astrolabe.Domain.Features.Support.Repositories;
using Astrolabe.Infrastructure.Persistence.Repositories.Support;

namespace Astrolabe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' is not configured. Set ConnectionStrings__Database.");

        services.AddDbContext<AstrolabeDbContext>(options => options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__migrations_history", AstrolabeDbContext.Schema))
            // PostgreSQL folds unquoted identifiers to lower case, so PascalCase column names would
            // have to be quoted everywhere. snake_case keeps raw SQL and index filters readable.
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AstrolabeDbContext>());
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddRepositories();
        services.AddIdentityServices(configuration);
        services.AddScoped<NetworkSeeder>();
        services.AddScoped<DemoAccountSeeder>();
        services.AddScoped<DemoDirectorySeeder>();
        services.AddScoped<MembershipSeeder>();
        services.AddScoped<CatalogSeeder>();

        // Scoped so the memoised scope dies with the request. BR-NET-011 requires a revoked
        // assignment to take effect on the next request.
        services.AddScoped<ILibraryScopeProvider, LibraryScopeProvider>();
        services.AddScoped<IEntitlementProvider, EntitlementProvider>();

        // BR-MBR-021 needs both mechanisms: the provider applies a due change on read, this sweeps
        // the members who never sign in. Both call the same idempotent method.
        services.Configure<PlanRenewalOptions>(
            configuration.GetSection(PlanRenewalOptions.SectionName));
        services.AddHostedService<ApplyDuePlanChangesJob>();

        // The event handler prices a fine immediately; this guarantees none is ever missed. A
        // post-commit reaction may be lost, and a lost one would be an unbilled fine.
        services.Configure<FineAccrualOptions>(
            configuration.GetSection(FineAccrualOptions.SectionName));
        services.AddHostedService<AssessOutstandingFinesJob>();
        services.AddScoped<ILibraryLocationProvider, LibraryLocationProvider>();

        // Placeholder until catalog, reservations and billing exist. See NET-025.
        services.AddScoped<ILibraryObligationsProbe, LibraryObligationsProbe>();
        services.AddScoped<IMemberActivityProbe, MemberActivityProbe>();

        // Recommendations. The two vendor clients are registered as the same interface and picked
        // apart by AiProviderRegistry, so adding a third provider is a registration rather than a
        // change to every caller.
        services.AddDataProtection();
        services.AddScoped<ISecretProtector, DataProtectionSecretProtector>();
        services.AddScoped<IReadingProfileBuilder, ReadingProfileBuilder>();
        services.AddScoped<IFallbackRecommender, MostBorrowedFallbackRecommender>();
        services.AddScoped<IRecommendationGenerator, RecommendationGenerator>();
        services.AddScoped<IAiRecommendationProvider, ClaudeRecommendationProvider>();
        services.AddScoped<IAiRecommendationProvider, OpenAiRecommendationProvider>();
        services.AddScoped<IAiProviderRegistry, AiProviderRegistry>();
        services.AddOptions<AiProviderOptions>()
            .Bind(configuration.GetSection(AiProviderOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddEmail(configuration);

        return services;
    }

    /// <summary>
    /// Scoped, matching the DbContext lifetime: a repository holding a longer-lived context would
    /// serve stale data and leak change tracking across requests.
    /// </summary>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<ISingleUseTokenRepository, SingleUseTokenRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        // One unit of work per bounded context. Scoped, so every repository it exposes shares the
        // request's change tracker and a single SaveChangesAsync commits them atomically.
        services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();
        services.AddScoped<INetworkUnitOfWork, NetworkUnitOfWork>();
        services.AddScoped<IMembershipUnitOfWork, MembershipUnitOfWork>();
        services.AddScoped<ICatalogUnitOfWork, CatalogUnitOfWork>();
        services.AddScoped<ILibraryAiConfigurationRepository, LibraryAiConfigurationRepository>();
        services.AddScoped<IRecommendationSetRepository, RecommendationSetRepository>();
        services.AddScoped<IRecommendationsUnitOfWork, RecommendationsUnitOfWork>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<INotificationsUnitOfWork, NotificationsUnitOfWork>();
        services.AddScoped<INotificationRaiser, NotificationRaiser>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ISupportUnitOfWork, SupportUnitOfWork>();
        services.AddScoped<IAuditUnitOfWork, AuditUnitOfWork>();
        services.AddScoped<IReservationUnitOfWork, ReservationUnitOfWork>();
        services.AddScoped<IBillingUnitOfWork, BillingUnitOfWork>();
        services.AddScoped<IStoreUnitOfWork, StoreUnitOfWork>();

        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IFineRepository, FineRepository>();
        services.AddScoped<ILedgerRepository, LedgerRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<IDeskPaymentRepository, DeskPaymentRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPointsRepository, PointsRepository>();

        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<ILibraryRepository, LibraryRepository>();
        services.AddScoped<ILibraryAssignmentRepository, LibraryAssignmentRepository>();
        services.AddScoped<IAdminInvitationRepository, AdminInvitationRepository>();

        return services;
    }

    /// <summary>
    /// Registers identity services. The hasher, token generator and device parser are stateless, so
    /// they are singletons; the revocation cache is a singleton because its whole purpose is to
    /// outlive individual requests.
    /// </summary>
    private static IServiceCollection AddIdentityServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();

        services.AddSingleton<IPasswordHasher, AspNetIdentityPasswordHasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IDeviceParser, UserAgentDeviceParser>();
        services.AddSingleton<ISessionRevocationCache, InMemorySessionRevocationCache>();

        return services;
    }

    /// <summary>
    /// Registers transactional email. Validated on start so a missing API key stops the process
    /// immediately rather than surfacing as a failed registration hours later.
    /// The sender is a singleton because it owns a pooled HTTP client.
    /// </summary>
    private static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<MailgunOptions>()
            .Bind(configuration.GetSection(MailgunOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IEmailSender, MailgunEmailSender>();

        return services;
    }
}

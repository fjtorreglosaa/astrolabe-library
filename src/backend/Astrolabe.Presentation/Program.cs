using System.Text.Json.Serialization;
using Astrolabe.Application;
using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Infrastructure;
using Astrolabe.Infrastructure.Persistence;
using Astrolabe.Presentation.Extensions;
using Astrolabe.Presentation.Identity;
using Astrolabe.Infrastructure.Realtime;
using Astrolabe.Presentation.Middleware;
using Astrolabe.Presentation.Options;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging. Configuration lives in appsettings so it varies by environment without a rebuild.
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services
    .AddOptions<FrontendOptions>()
    .Bind(builder.Configuration.GetSection(FrontendOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Claims are an HTTP concern, so the current-user reader is registered by the layer that owns them.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddAstrolabeAuthentication(builder.Configuration);
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Enumerations travel as their names, never as ordinals. A numeric plan or role in a payload
    // is unreadable in a log, and reordering an enum would silently reinterpret stored requests.
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Liveness answers "is the process up". Readiness additionally answers "can it serve traffic",
// which for this API means the database is reachable. See GUIDELINES.md section 29.
builder.Services
    .AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Database")!,
        name: "database",
        tags: ["ready"]);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration
            .GetSection(FrontendOptions.SectionName)
            .Get<FrontendOptions>()?.AllowedOrigins ?? [];

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();

// Runs after authentication so the claims exist, and before authorization so no endpoint ever sees
// a revoked identity. This is what makes BR-IDN-023 true in practice.
app.UseMiddleware<SessionValidationMiddleware>();

app.UseAuthorization();

// After authentication, so it can tell an authenticated response from an anonymous one, and before
// the endpoints, so it covers every one of them without an attribute per route.
app.UseMiddleware<NoStoreForAuthenticatedResponsesMiddleware>();
app.MapControllers();

// Mounted here rather than in Infrastructure so the hub sits behind the same pipeline as every
// controller: CORS, authentication, the session check that enforces BR-IDN-023, then authorization.
// A revoked session therefore cannot open a connection, which is the point of putting it after
// SessionValidationMiddleware rather than in front of it.
app.MapHub<RealtimeHub>(HubRoutes.Realtime);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    // Liveness must not depend on the database, or a database blip would restart a healthy process.
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

await app.ApplyMigrationsAsync();
await app.SeedAsync();

await app.RunAsync();

/// <summary>
/// Exposed so the integration test host can reference the entry point assembly.
/// </summary>
public partial class Program;

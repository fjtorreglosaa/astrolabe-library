using System.Text;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Infrastructure.Features.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Astrolabe.Presentation.Extensions;

/// <summary>
/// Configures JWT bearer authentication and the authorization policies.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddAstrolabeAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

                    // Containers share the host clock, so a wide allowance would only extend the
                    // life of an expired token.
                    ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds)
                };

                options.Events = new JwtBearerEvents
                {
                    // A browser cannot set an Authorization header on a WebSocket handshake — the
                    // API simply does not exist — so SignalR's own convention is to put the token in
                    // the query string. This reads it for the hub path and nowhere else.
                    //
                    // Narrowing it to that path matters. A token in a URL is a token in proxy logs,
                    // in browser history and in any Referer the page emits; accepting one on the
                    // REST surface would invite exactly that, and the REST surface has a header
                    // available. Validation is unchanged either way — the same signature, issuer,
                    // audience and lifetime checks configured above.
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(token) &&
                            context.HttpContext.Request.Path.StartsWithSegments(HubRoutes.Realtime))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            // nameof, not literals. These strings must match what JwtTokenGenerator writes into
            // the role claim, and GLOBAL-019 renamed the enum underneath three literals that would
            // have compiled perfectly and locked every member out at run time.
            options.AddPolicy(Policies.MemberOnly, policy =>
                policy.RequireRole(nameof(UserRole.Member)));

            options.AddPolicy(Policies.StaffOnly, policy =>
                policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.SuperAdmin)));

            options.AddPolicy(Policies.SuperAdminOnly, policy =>
                policy.RequireRole(nameof(UserRole.SuperAdmin)));
        });

        return services;
    }
}

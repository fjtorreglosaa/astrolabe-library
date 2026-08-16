using System.Text;
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
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.MemberOnly, policy =>
                policy.RequireRole("Basic", "Plus", "Max"));

            options.AddPolicy(Policies.StaffOnly, policy =>
                policy.RequireRole("Admin", "SuperAdmin"));

            options.AddPolicy(Policies.SuperAdminOnly, policy =>
                policy.RequireRole("SuperAdmin"));
        });

        return services;
    }
}

/// <summary>Authorization policy names, declared once so no controller invents a string.</summary>
public static class Policies
{
    public const string MemberOnly = "MemberOnly";
    public const string StaffOnly = "StaffOnly";
    public const string SuperAdminOnly = "SuperAdminOnly";
}

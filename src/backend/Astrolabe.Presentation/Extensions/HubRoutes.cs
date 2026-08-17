namespace Astrolabe.Presentation.Extensions;

/// <summary>
/// Where the hubs are mounted.
/// </summary>
/// <remarks>
/// A constant because two places must agree on it and they are far apart: the endpoint mapping in
/// <c>Program</c>, and the authentication rule that accepts a token from the query string only on
/// this path. If those two ever disagreed, the failure is a hub that authenticates nobody — or, far
/// worse, a REST surface that starts accepting tokens out of URLs.
/// </remarks>
public static class HubRoutes
{
    public const string Realtime = "/hubs/realtime";
}

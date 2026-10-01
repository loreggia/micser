using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Micser.Engine.Security;

/// <summary>
/// The access token of this engine instance. It's generated at startup and published in the discovery file, which
/// only the current user can read; requests to /api and /hubs must carry it.
/// </summary>
public sealed class EngineAccess
{
    private readonly byte[] _tokenBytes = Encoding.ASCII.GetBytes(WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)));

    public EngineAccess(IOptions<EngineOptions> options)
    {
        RequireToken = options.Value.RequireToken;
    }

    public bool RequireToken { get; }

    public string Token => Encoding.ASCII.GetString(_tokenBytes);

    public bool IsValid(string? token)
    {
        return token != null && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(token), _tokenBytes);
    }
}

public static class EngineAccessMiddleware
{
    /// <summary>
    /// Rejects /api and /hubs requests without the access token, except the health check. The token is read from the
    /// <c>Authorization: Bearer</c> header or, for SignalR connections from browsers, the <c>access_token</c> query parameter.
    /// </summary>
    public static IApplicationBuilder UseEngineAccess(this IApplicationBuilder app)
    {
        var access = app.ApplicationServices.GetRequiredService<EngineAccess>();
        if (!access.RequireToken)
        {
            return app;
        }

        return app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            var isProtected = (path.StartsWithSegments("/api") || path.StartsWithSegments("/hubs")) && !path.StartsWithSegments("/api/health");

            if (isProtected && !access.IsValid(GetToken(context.Request)))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await next(context);
        });
    }

    private static string? GetToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return header["Bearer ".Length..].Trim();
        }

        return request.Query["access_token"].FirstOrDefault();
    }
}

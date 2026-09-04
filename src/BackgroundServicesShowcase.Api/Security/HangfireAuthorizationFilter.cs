using System.Text;
using BackgroundServicesShowcase.Api.Configuration;
using Hangfire.Dashboard;

namespace BackgroundServicesShowcase.Api.Security;

public sealed class HangfireAuthorizationFilter(HangfireDashboardConfig config) : IDashboardAuthorizationFilter
{
    private readonly HangfireDashboardConfig _config = config;

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext.Request.Headers.TryGetValue("Authorization", out var headerValue)
            && TryParseBasicAuth(headerValue.ToString(), out var username, out var password)
            && username == _config.Username
            && password == _config.Password)
        {
            return true;
        }

        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire Dashboard\"";
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return false;
    }

    private static bool TryParseBasicAuth(string headerValue, out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        if (!headerValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var encoded = headerValue["Basic ".Length..].Trim();
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var separatorIndex = decoded.IndexOf(':');

            if (separatorIndex < 0)
            {
                return false;
            }

            username = decoded[..separatorIndex];
            password = decoded[(separatorIndex + 1)..];
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

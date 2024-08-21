using BeybladeTournamentManager.Components;

public class AccessTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccessTokenMiddleware(RequestDelegate next, IHttpContextAccessor httpContextAccessor)
    {
        _next = next;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await CheckOAuth2Token(context);
        await _next(context);
    }

    private async Task CheckOAuth2Token(HttpContext context)
    {
        var accessToken = context.Session.GetString("GoogleAccessToken");
        var refreshToken = context.Session.GetString("GoogleRefreshToken");

        if (string.IsNullOrEmpty(accessToken))
        {
            var tokenFilePath = Path.Combine(Directory.GetCurrentDirectory(), "AccessToken.json");
            if (File.Exists(tokenFilePath))
            {
                accessToken = TokenEncryption.LoadToken(tokenFilePath);
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Session.SetString("GoogleAccessToken", accessToken);
                }
            }
        }

        if (string.IsNullOrEmpty(accessToken) && !context.Request.Path.StartsWithSegments("/api/auth"))
        {
            context.Response.Redirect("/api/auth/google-login");
            return;
        }

        // Check if the access token is valid
        if (!await IsAccessTokenValid(accessToken))
        {
            // Refresh the access token using the refresh token
            var newAccessToken = await RefreshAccessToken(refreshToken);
            if (!string.IsNullOrEmpty(newAccessToken))
            {
                context.Session.SetString("GoogleAccessToken", newAccessToken);
            }
            else
            {
                context.Response.Redirect("/api/auth/google-login");
                return;
            }
        }
    }

    private async Task<bool> IsAccessTokenValid(string accessToken)
    {
        // Implement a method to check if the access token is valid
        // This could involve making a test API call to Google Sheets or another Google API
        // For simplicity, let's assume the token is valid if it's not empty
        return !string.IsNullOrEmpty(accessToken);
    }

    private async Task<string> RefreshAccessToken(string refreshToken)
    {
        // Implement a method to refresh the access token using the refresh token
        // This involves making a request to the OAuth 2.0 token endpoint
        // For simplicity, let's assume we get a new access token
        return await Task.FromResult("new-access-token");
    }
}
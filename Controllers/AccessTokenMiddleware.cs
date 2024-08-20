using BeybladeTournamentManager.Components.Pages.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BeybladeTournamentManager.Components
{
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

            await _next(context);
        }

        public string LoadTokenAsync()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("GoogleAccessToken");
        }
    }
}
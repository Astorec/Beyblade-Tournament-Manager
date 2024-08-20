using BeybladeTournamentManager.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;

[Route("api/auth")]
public class AuthController : Controller
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        var redirectUrl = Url.Action(nameof(GoogleResponse));
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            _logger.LogError("Google authentication failed: {0}", authenticateResult.Failure?.Message);
            return BadRequest(); // Handle error
        }

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, authenticateResult.Principal.Identity.Name),
        // Add other claims as needed
    };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties
     = new AuthenticationProperties
     {
         IsPersistent = true,
         // Make the cookie persistent
         ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) // Set the expiration time
     };

        var accessToken = authenticateResult.Properties.GetTokenValue("access_token");
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogError("Access token is null or empty.");
            return BadRequest("Access token is null or empty."); // Handle error
        }

        // Store access token in session (assuming TokenService retrieves from session)
        HttpContext.Session.SetString("GoogleAccessToken", accessToken);

        var refreshToken = authenticateResult.Properties.GetTokenValue("refresh_token");
        if (!string.IsNullOrEmpty(refreshToken))
        {
            HttpContext.Session.SetString("GoogleRefreshToken", refreshToken);
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity),
     authProperties);

        // Save the access token to a file
        TokenEncryption.SaveToken("AccessToken.json", accessToken);

        return Redirect("/"); // Redirect to the home page or another page
    }
}
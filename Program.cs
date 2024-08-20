using System.Text.Json;
using BeybladeTournamentManager.ApiCalls.Challonge;
using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using BeybladeTournamentManager.ApiCalls.Google;
using BeybladeTournamentManager.Components;
using BeybladeTournamentManager.Components.Pages.ViewModels;
using BeybladeTournamentManager.Config;
using BeybladeTournamentManager.Helpers;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var userSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.user.json");
if (File.Exists(userSettingsPath))
{
    builder.Configuration.AddJsonFile(userSettingsPath, optional: true, reloadOnChange: true);
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddControllers();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/api/auth/google-login";
    options.LogoutPath = "/api/auth/logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Set the cookie expiration time
    options.SlidingExpiration = true;
})
    .AddGoogle(options =>
    {
        IConfigurationSection googleAuthNSection =
            builder.Configuration.GetSection("Authentication:Google");

        options.ClientId = googleAuthNSection["ClientId"];
        options.ClientSecret = googleAuthNSection["ClientSecret"];
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.Scope.Add("https://www.googleapis.com/auth/drive.file");
        options.Scope.Add("https://www.googleapis.com/auth/spreadsheets");
        options.Scope.Add("https://www.googleapis.com/auth/drive");
        options.SaveTokens = true;
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddSingleton<Challonge.Api.ChallongeCredentials>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiKey = configuration.GetValue<string>("ChallongeUsername");
    var username = configuration.GetValue<string>("ChallongeAPIKey");
    return new Challonge.Api.ChallongeCredentials(username, apiKey);
});


builder.Services.AddScoped<ISettingsViewModel, SettingsViewModel>();
builder.Services.AddAuthorizationCore();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPlayersViewModel, PlayersViewModel>();
builder.Services.AddScoped<ITournamentViewModel, TournamentViewModel>();
builder.Services.AddScoped<ISpreadsheetViewModel, SpreadsheetViewModel>();

builder.Services.AddScoped<IAutentication, Authentication>();
builder.Services.AddSingleton<IGoogleServiceFactory, GoogleServiceFactory>();
builder.Services.AddScoped<IGoogleService, GoogleService>();
builder.Services.AddScoped<IMatches, Matches>();
builder.Services.AddScoped<ITournamentManager, TournamentManager>();
builder.Services.AddScoped<IParticipants, Participants>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var configFilePath = Path.Combine(app.Environment.ContentRootPath, "appsettings.user.json");
if (!File.Exists(configFilePath))
{
    var defaultSettings = new AppSettings
    {
        ChallongeAPIKey = "",
        ChallongeUsername = "",
        GoogleAppName = "",
        SheetID = ""
    };

    var settingsJson = JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(configFilePath, settingsJson);
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AccessTokenMiddleware>();

app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
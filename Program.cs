using System.Text.Json;
using BeybladeTournamentManager.ApiCalls.Challonge;
using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using BeybladeTournamentManager.ApiCalls.Google;
using BeybladeTournamentManager.Components;
using BeybladeTournamentManager.Config;
using BeybladeTournamentManager.Helpers;
using MudBlazor.Services;



var builder = WebApplication.CreateBuilder(args);

var userSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.user.json");
if (File.Exists(userSettingsPath))
{
    builder.Configuration.AddJsonFile(userSettingsPath, optional: true, reloadOnChange: true);
}


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddScoped<IAutentication, Authentication>();
builder.Services.AddScoped<IPlayerHelper, PlayerHelper>();
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
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

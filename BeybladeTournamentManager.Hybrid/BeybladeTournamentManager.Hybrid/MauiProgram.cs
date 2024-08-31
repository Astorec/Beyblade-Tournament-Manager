using Microsoft.Extensions.Logging;

namespace BeybladeTournamentManager.Hybrid;
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

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
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


				string clientId = Environment.GetEnvironmentVariable("TOKEN");
				string clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");

				// Add logging to verify the values
				Console.WriteLine($"ClientId: {clientId}");
				Console.WriteLine($"ClientSecret: {clientSecret}");

				if (string.IsNullOrEmpty(clientId))
				{
					throw new ArgumentException("The 'ClientId' option must be provided.", nameof(clientId));
				}

				if (string.IsNullOrEmpty(clientSecret))
				{
					throw new ArgumentException("The 'ClientSecret' option must be provided.", nameof(clientSecret));
				}

				options.ClientId = clientId;
				options.ClientSecret = clientSecret;
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

		builder.Services.AddAuthorizationCore();
		builder.Services.AddHttpContextAccessor();
		builder.Services.AddScoped<ISettingsViewModel, SettingsViewModel>();

		builder.Services.AddScoped<IPlayersViewModel, PlayersViewModel>();
		builder.Services.AddScoped<ITournamentViewModel, TournamentViewModel>();
		builder.Services.AddScoped<ISpreadsheetViewModel, SpreadsheetViewModel>();

		builder.Services.AddScoped<IAutentication, Authentication>();
		builder.Services.AddSingleton<IGoogleServiceFactory, GoogleServiceFactory>();
		builder.Services.AddScoped<IGoogleService, GoogleService>();
		builder.Services.AddScoped<IMatches, Matches>();
		builder.Services.AddScoped<ITournamentManager, TournamentManager>();
		builder.Services.AddScoped<IParticipants, Participants>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

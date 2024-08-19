using System.Text.Json;
using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using BeybladeTournamentManager.Config;
using Challonge.Api;

namespace BeybladeTournamentManager.Components.Pages.ViewModels
{
    public class SettingsViewModel : ISettingsViewModel
    {
        private AppSettings _appSettings;
        private static ChallongeClient _client;
        private static ChallongeCredentials _creds;
        public SettingsViewModel(ChallongeCredentials creds)
        {
            var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.user.json");
            _appSettings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(configFilePath));
            _creds = creds;
        }

        public AppSettings LoadSettings()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true);

                IConfigurationRoot configuration = builder.Build();

                var settings = new AppSettings
                {
                    ChallongeAPIKey = configuration["ChallongeAPIKey"],
                    ChallongeUsername = configuration["ChallongeUsername"],
                    GoogleAppName = configuration["GoogleAppName"],
                    CurrentTournament = "",
                    CurrentTournamentDetails = new TournamentDetails(),
                    SheetID = configuration["SheetID"],
                    PreviousTournements = configuration.GetSection("PreviousTournements").Get<Dictionary<string, string>>(),
                };


                Console.WriteLine($"ChallongeUsername: {settings.ChallongeUsername}");
                return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                return null;
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            _appSettings = settings;
            var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.user.json");
            var settingsJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFilePath, settingsJson);

            if (_creds != null)
            {
                _creds.Username = settings.ChallongeUsername;
                _creds.ApiKey = settings.ChallongeAPIKey;

            }
            else
            {
                _creds = new ChallongeCredentials(settings.ChallongeUsername, settings.ChallongeAPIKey);
            }


            _client = new ChallongeClient(new HttpClient(), _creds);
        }

        public AppSettings GetSettings => _appSettings;
    }
}
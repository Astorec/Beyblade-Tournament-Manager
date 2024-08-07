using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using Challonge.Api;
using Challonge.Objects;
using BeybladeTournamentManager.Config;
using BeybladeTournamentManager.Components.Pages;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public class Authentication : IAutentication
    {
        private static AppSettings _settings;
        private static ChallongeCredentials _creds;
        private static ChallongeClient _client;

        public Authentication()
        {
            var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.user.json");
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(configFilePath));
            if (settings.ChallongeUsername != "" && settings.ChallongeAPIKey != "")
            {
                _creds = new ChallongeCredentials(settings.ChallongeUsername, settings.ChallongeAPIKey);
                _client = new ChallongeClient(new HttpClient(), _creds);
            }

            _settings = LoadSettings();
        }

        public ChallongeClient GetClient()
        {
            return _client;
        }

        public AppSettings GetSettings()
        {
            return _settings;
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

                var settings = new AppSettings{
                    ChallongeAPIKey = configuration["ChallongeAPIKey"],
                    ChallongeUsername = configuration["ChallongeUsername"],
                    GoogleAppName = configuration["GoogleAppName"],
                    SheetID = configuration.GetSection("SheetID").Get<List<string>>(),
                    SheetNames = configuration.GetSection("SheetNames").Get<List<string>>(),
                    CurrentTournament = configuration["CurrentTournament"]
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
            if (settings.CurrentTournament == null || settings.CurrentTournament != "")
            {
                if (_settings.CurrentTournament != null && _settings.CurrentTournament != "")
                    settings.CurrentTournament = _settings.CurrentTournament;
            }

            _settings = settings;
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
    }
}
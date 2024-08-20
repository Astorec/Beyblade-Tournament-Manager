using System.Security.Cryptography;
using System.Text.Json;
using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using BeybladeTournamentManager.Config;
using Challonge.Api;
using Microsoft.Extensions.Options;

namespace BeybladeTournamentManager.Components.Pages.ViewModels
{
    public class SettingsViewModel : ISettingsViewModel
    {
        private AppSettings _appSettings;
        private static ChallongeClient _client;
        private static ChallongeCredentials _creds;
        private readonly IOptionsMonitor<AppSettings> _settingsMonitor;
        private static readonly ConfigurationProvider _configProvider;
        private readonly IConfiguration _configuration;

        public SettingsViewModel(ChallongeCredentials creds, IOptionsMonitor<AppSettings> settingsMonitor, IConfiguration configuration)
        {
            var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.user.json");
            _appSettings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(configFilePath));
            _creds = creds;
            _configuration = configuration;
            _settingsMonitor = settingsMonitor;
            _settingsMonitor.OnChange(OnSettingsChanged);

            LoadSettings();
        }

        private void OnSettingsChanged(AppSettings settings)
        {
            // React to settings changes
            Console.WriteLine("Settings changed!");
        }

        public AppSettings LoadSettings()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true);

                IConfigurationRoot configuration = builder.Build();

                var settings = new AppSettings
                {
                    EncryptionKey = configuration["EncryptionKey"],
                    ChallongeAPIKey = configuration["ChallongeAPIKey"],
                    ChallongeUsername = configuration["ChallongeUsername"],
                    GoogleAppName = configuration["GoogleAppName"],
                    CurrentTournament = configuration["CurrentTournament"],
                    CurrentTournamentDetails = configuration.GetSection("CurrentTournamentDetails").Get<TournamentDetails>(),
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
            // Update _appSettings with the new settings
            _appSettings = new AppSettings
            {
                EncryptionKey = settings.EncryptionKey,
                ChallongeAPIKey = settings.ChallongeAPIKey,
                ChallongeUsername = settings.ChallongeUsername,
                GoogleAppName = settings.GoogleAppName,
                CurrentTournament = settings.CurrentTournament,
                CurrentTournamentDetails = settings.CurrentTournamentDetails,
                SheetID = settings.SheetID,
                PreviousTournements = settings.PreviousTournements,
            };
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
            var configurationRoot = (IConfigurationRoot)_configuration;
            configurationRoot.Reload();
        }

        public AppSettings GetSettings => _appSettings;


        private string GenerateEncryptionKey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] key = new byte[32]; // 256 bits
                rng.GetBytes(key);
                return Convert.ToBase64String(key);
            }
        }
    }
}
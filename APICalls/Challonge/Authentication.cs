using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using Challonge.Api;
using Challonge.Objects;
using BeybladeTournamentManager.Config;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public class Authentication : IAutentication
    {
        private static ChallongeCredentials _creds;
        private static ChallongeClient _client;
      
        public Authentication()
        {
            var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.user.json");
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(configFilePath));
            if(settings.ChallongeUsername != "" && settings.ChallongeAPIKey != "")
            {
                _creds = new ChallongeCredentials(settings.ChallongeUsername, settings.ChallongeAPIKey);
                _client = new ChallongeClient(new HttpClient(), _creds);
            }
        }

        public ChallongeClient GetClient()
        {
            return _client;
        }

        public void SaveSettings(AppSettings settings)
        {
            var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.user.json");
            var settingsJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFilePath, settingsJson);

            if(_creds != null)
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
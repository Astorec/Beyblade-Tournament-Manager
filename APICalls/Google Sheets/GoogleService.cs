using Google.Apis.Services;
using BeybladeTournamentManager.Config;
using BeybladeTournamentManager.ApiCalls.Challonge;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;

namespace BeybladeTournamentManager.ApiCalls.Google
{
    public class GoogleService : IGoogleService
    {
        private static AppSettings _settings;
        private readonly IAutentication _auth;
        private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

        private static SheetsService _service;
        public GoogleService(IAutentication auth)        
        {
            _auth = auth;
            _settings = _auth.GetSettings();
            GoogleCredential credential;
            
            // Check AppSettings for GoogleCredLocation
            // If it exists, use it to create a GoogleCredential
            if (_settings.GoogleCredLocation != "")
            {
                using (var stream = new FileStream("tmp/beyblde-tournament-manager-c86c4d40deec.json", FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(Scopes);
                }

                _service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = _settings.GoogleAppName
                });
            }
        }

        public SheetsService GetService()
        {
            if(_service != null)
            {
                return _service;
            }
            else
            {
                throw new Exception("GoogleService not initialized");
            }
        }
        
    }
}
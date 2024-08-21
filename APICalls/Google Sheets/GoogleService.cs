using Google.Apis.Services;
using BeybladeTournamentManager.Config;
using BeybladeTournamentManager.ApiCalls.Challonge;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using NuGet.Configuration;
using BeybladeTournamentManager.Components.Pages.ViewModels;

namespace BeybladeTournamentManager.ApiCalls.Google
{
    public class GoogleService : IGoogleService
    {
        private static AppSettings _settings;
        private readonly IAutentication _auth;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

        private static SheetsService _service;
        public GoogleService(IAutentication auth, ISettingsViewModel settingsViewModel, IHttpContextAccessor httpContextAccessor)
        {
            _auth = auth;
            _settings = settingsViewModel.GetSettings;
            _httpContextAccessor = httpContextAccessor;

            var accessToken = _httpContextAccessor.HttpContext.Session.GetString("GoogleAccessToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("Google token is not ready yet");
            }
            else
            {
                var credential = GoogleCredential.FromAccessToken(accessToken)
                            .CreateScoped(Scopes);

                _service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = _settings.GoogleAppName
                });
            }


        }

        public SheetsService GetService()
        {
            if (_service != null)
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
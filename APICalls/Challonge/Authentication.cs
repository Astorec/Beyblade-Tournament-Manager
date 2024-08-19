using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using Challonge.Api;
using Challonge.Objects;
using BeybladeTournamentManager.Config;
using System.Diagnostics;
using BeybladeTournamentManager.Components.Pages.ViewModels;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public class Authentication : IAutentication
    {
        private static AppSettings _settings;
        private static ChallongeCredentials _creds;
        private static ChallongeClient _client;

        public Authentication(ISettingsViewModel settingsViewModel)
        {
            _settings = settingsViewModel.LoadSettings();


            if (_settings.ChallongeUsername != "" && _settings.ChallongeAPIKey != "")
            {
                _creds = new ChallongeCredentials(_settings.ChallongeUsername, _settings.ChallongeAPIKey);
                _client = new ChallongeClient(new HttpClient(), _creds);
            }

        }

        public ChallongeClient GetClient()
        {
            return _client;
        }


    }
}
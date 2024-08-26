using System.Text.RegularExpressions;
using BeybladeTournamentManager.ApiCalls.Challonge;
using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using BeybladeTournamentManager.Config;
using BeybladeTournamentManager.Helpers;
using Challonge.Api;
using Challonge.Objects;
using Humanizer;

namespace BeybladeTournamentManager.Components.Pages.ViewModels
{
    public class TournamentViewModel : ITournamentViewModel
    {
        ChallongeClient? _client;
        private readonly IAutentication _autentication;
        private ISpreadsheetViewModel _spreadsheetViewModel;
        private ISettingsViewModel _settingsViewModel;
        private IPlayersViewModel _playersViewModel;
        private ITournamentManager _tournamentManger;
        private AppSettings _appSettings;
        public TournamentViewModel(ISettingsViewModel settingsViewModel, ISpreadsheetViewModel spreadsheetViewModel, IPlayersViewModel playerVM, ITournamentManager manager, IAutentication auth)
        {
            _autentication = auth;
            _spreadsheetViewModel = spreadsheetViewModel;
            _client = _autentication.GetClient();
            _settingsViewModel = settingsViewModel;
            _appSettings = _settingsViewModel.GetSettings;
            _tournamentManger = manager;
            _playersViewModel = playerVM;
        }
        public async Task AddedNewTournament(bool added)
        {
            if (added)
            {
                await GetParticipentsViaURL(TournamentDetails.tournamentUrl);
            }
        }
        public async Task HandleUrlAdded(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                // Handle the case where the URL is null or empty
                Console.WriteLine("URL is null or empty.");
                return;
            }
            var currentSettings = _settingsViewModel.GetSettings;

            var code = GetTournamentCode(url);

            if (_appSettings.PreviousTournements == null)
            {
                _appSettings.PreviousTournements = new System.Collections.Generic.Dictionary<string, string>();
            }
            if (!_appSettings.PreviousTournements.ContainsKey(code))
            {
                _appSettings.PreviousTournements.Add(code, url);
                currentSettings.PreviousTournements = _appSettings.PreviousTournements;
            }


            // Get the tournament info so we can save it to the settings file. This is so we can at least
            // access the spreadsheet name as it will error otherwise when trying to get the sheet information
            // to get the correct sheet info.
            var tourneyInfo = await _client.GetTournamentByUrlAsync(code);
            string startDate = "";

            startDate = tourneyInfo.StartedAt.Value.Date.ToString("dd/MM/yyyy");

            string sheetName = $"{tourneyInfo.Name} - {startDate}";

            TournamentDetails = _tournamentManger.SetTournamentDetails(code, tourneyInfo.Name, sheetName);
            currentSettings.CurrentTournamentDetails = TournamentDetails;

            _settingsViewModel.SaveSettings(currentSettings);


            // Get the participants
            await GetParticipentsViaURL(code);
        }

        private async Task GetParticipentsViaURL(string code)
        {
            Console.WriteLine("In GetParticipentsViaURL");
            if (_client == null)
            {
                return;
            }

            if (code != "" && _client != null)
            {
                _playersViewModel.isLoading = true;
                try
                {
                    Console.WriteLine("In Try of GetParticipentsViaURL");
                    if (_playersViewModel.PlayerCache.ContainsKey(code))
                    {
                        Console.WriteLine("In If of GetParticipentsViaURL");
                        var cachedPlayers = _playersViewModel.PlayerCache[code];
                        Console.WriteLine("Got Cached Players");
                        bool areEqual = _playersViewModel.Players.SequenceEqual(cachedPlayers);
                        Console.WriteLine("Checked if players are equal");
                        if (!areEqual)
                        {
                            Console.WriteLine("Players are not equal");
                            _playersViewModel.ClearPlayers();

                            Console.WriteLine("Cleared Players");
                            _playersViewModel.SetPlayers(cachedPlayers);
                        }

                    }
                    else
                    {

                        Console.WriteLine("In Else of GetParticipentsViaURL");
                        var participants = await _client.GetParticipantsAsync(code);

                        Console.WriteLine("Got Participants");
                        List<Participant> participantsList = participants.ToList();

                        _playersViewModel.ClearPlayers();
                        await AddPlayerFromParticipant(participantsList);
                        _playersViewModel.SetPlayers(_playersViewModel.Players);

                        // Create a new list instance to avoid reference issues
                        var playersCopy = new List<Player>(_playersViewModel.Players);
                        _playersViewModel.PlayerCache[code] = playersCopy;
                    }

                    Console.WriteLine("Setting current tournament details");
                    var currentSettings = _appSettings;
                    TournamentDetails.tournamentUrl = code;
                    currentSettings.CurrentTournamentDetails = TournamentDetails;
                    _settingsViewModel.SaveSettings(currentSettings);
                    Console.Write(_playersViewModel.Players.Count);
                    _playersViewModel.isLoading = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
        }

        private async Task AddPlayerFromParticipant(List<Participant> participants)
        {
            var tournament = await _client.GetTournamentByUrlAsync(_appSettings.CurrentTournamentDetails.tournamentUrl);
            var matches = await _client.GetMatchesAsync(tournament);
            List<Player> tempList = new List<Player>();

            // Update settings to the latest
            _appSettings = _settingsViewModel.GetSettings;
            foreach (var participant in participants)
            {
                Player p = new Player
                {
                    Name = participant.Name,
                    ChallongeId = participant.Id,
                    CheckInState = participant.CheckedIn,
                    CheckInTime = participant.CheckedInAt
                };

                await _spreadsheetViewModel.AddNewPlayer(_appSettings.CurrentTournamentDetails.relatedSheetName, p);
                if (tournament.State == TournamentState.Underway || tournament.State == TournamentState.Complete)
                {
                    // From matches find the players Wins and Losses
                    var playerMatches = matches.Where(x => x.Player1Id == participant.Id || x.Player2Id == participant.Id).ToList();

                    foreach (var match in playerMatches)
                    {
                        if (match.State != MatchState.Complete)
                        {
                            continue;
                        }
                        if (match.WinnerId == participant.Id && match.State == MatchState.Open)
                        {
                            p.Wins++;
                        }
                        else if (match.State == MatchState.Open)
                        {
                            p.Losses++;
                        }
                    }
                }
            }
        }

        public async Task StartTournament()
        {
            var tournament = await _client.GetTournamentByUrlAsync(_appSettings.CurrentTournamentDetails.tournamentUrl);
            await _client.StartTournamentAsync(tournament);
        }
        private string GetTournamentCode(string url)
        {
            string pattern = @"(?<=\.com/).*$";
            Console.WriteLine(Regex.Match(url, pattern).Value);
            return Regex.Match(url, pattern).Value;
        }


        public TournamentDetails TournamentDetails { get; set; }
    }
}
using System.Text.RegularExpressions;
using BeybladeTournamentManager.ApiCalls.Challonge;
using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using BeybladeTournamentManager.Config;
using BeybladeTournamentManager.Helpers;
using Challonge.Api;
using Challonge.Objects;
using Humanizer;
using Match = Challonge.Objects.Match;

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

            if (tourneyInfo.StartedAt.HasValue)
                startDate = tourneyInfo.StartedAt.Value.Date.ToString("dd/MM/yyyy");
            else
                startDate = tourneyInfo.CreatedAt.Date.ToString("dd/MM/yyyy");
                
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

                Console.WriteLine("Setting current tournament details");
                var currentSettings = _appSettings;
                TournamentDetails.tournamentUrl = code;
                currentSettings.CurrentTournamentDetails = TournamentDetails;
                TournamentDetails td = new TournamentDetails
                {
                    tournamentUrl = code,
                    tournamentName = TournamentDetails.tournamentName,
                    relatedSheetName = TournamentDetails.relatedSheetName,
                    isCompleted = false
                };

                if (currentSettings.TournamentDetails == null)
                {
                    currentSettings.TournamentDetails = new List<TournamentDetails>();
                }
                currentSettings.TournamentDetails.Add(td);
                _settingsViewModel.SaveSettings(currentSettings);
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

            if (_appSettings.CurrentTournamentDetails == null)
            {
                _appSettings.CurrentTournamentDetails = new TournamentDetails();
            }
            if (tournament.State == TournamentState.Complete)
            {
                var currentSettings = _appSettings;
                if (currentSettings.TournamentDetails == null)
                {
                    currentSettings.TournamentDetails = new List<TournamentDetails>();

                    var newDetails = new TournamentDetails
                    {
                        tournamentUrl = _appSettings.CurrentTournamentDetails.tournamentUrl,
                        tournamentName = _appSettings.CurrentTournamentDetails.tournamentName,
                        relatedSheetName = _appSettings.CurrentTournamentDetails.relatedSheetName,
                        isCompleted = true
                    };

                    currentSettings.TournamentDetails.Add(newDetails);
                }
                else
                {
                    var currentTournament = currentSettings.TournamentDetails.Find(x => x.tournamentUrl == _appSettings.CurrentTournamentDetails.tournamentUrl);
                    currentTournament.isCompleted = true;

                    // update settings with the new details
                    currentSettings.TournamentDetails.Remove(currentTournament);
                    currentSettings.TournamentDetails.Add(currentTournament);
                }
                var tournamentDetails = currentSettings.TournamentDetails;
                currentSettings.TournamentDetails = tournamentDetails;
                _settingsViewModel.SaveSettings(currentSettings);
            }

            // Update settings to the latest
            _appSettings = _settingsViewModel.GetSettings;
            if (!_appSettings.CurrentTournamentDetails.addedToMainSheet)
            {
                foreach (var participant in participants)
                {
                    Player p = new Player
                    {
                        Name = participant.Name,
                        region = "",
                        ChallongeId = participant.Id,
                        CheckInState = participant.CheckedIn,
                        CheckInTime = participant.CheckedInAt
                    };

                    if (tournament.State == TournamentState.Underway || tournament.State == TournamentState.Complete)
                    {
                        foreach (Match match in matches)
                        {
                            if (participant.Id == match.WinnerId || participant.GroupPlayerIds.Any(x => x == match.WinnerId))
                            {
                                p.Wins += 1;
                            }
                            else if (participant.Id == match.LoserId || participant.GroupPlayerIds.Any(x => x == match.LoserId))
                            {
                                p.Losses += 1;
                            }
                        }
                    }

                    _playersViewModel.Players.Add(p);
                }
            }

            await _spreadsheetViewModel.AddNewPlayers(_appSettings.CurrentTournamentDetails.relatedSheetName, _playersViewModel.Players);

            if (_appSettings.TournamentDetails != null && _appSettings.TournamentDetails.Find(x => x.tournamentUrl == _appSettings.CurrentTournamentDetails.tournamentUrl).isCompleted
            && !_appSettings.CurrentTournamentDetails.addedToMainSheet)
            {

                await _spreadsheetViewModel.UpdatePlayersInMainSheet(_playersViewModel.Players);
                _appSettings.TournamentDetails.Find(x => x.tournamentUrl == _appSettings.CurrentTournamentDetails.tournamentUrl).addedToMainSheet = true;
                _appSettings.CurrentTournamentDetails.addedToMainSheet = true;
                _settingsViewModel.SaveSettings(_appSettings);
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
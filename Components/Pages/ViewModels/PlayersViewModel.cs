using BeybladeTournamentManager.ApiCalls.Challonge;
using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using BeybladeTournamentManager.Config;
using BeybladeTournamentManager.Helpers;
using Challonge.Api;
using Challonge.Objects;
using NuGet.Configuration;

namespace BeybladeTournamentManager.Components.Pages.ViewModels
{
    public class PlayersViewModel : IPlayersViewModel
    {
        ChallongeClient? _client;
        private readonly IAutentication _autentication;
        private ISettingsViewModel _settingsViewModel;
        private ISpreadsheetViewModel _spreadsheetViewModel;
        private AppSettings _appSettings;
        Dictionary<string, List<Player>> playerCache = new Dictionary<string, List<Player>>();
        public event Action OnStateChanged;
        private bool _isLoading = true;
        public PlayersViewModel(IAutentication auth, ISettingsViewModel settingsViewModel, ISpreadsheetViewModel spreadsheetViewModel)
        {
            _autentication = auth;
            _client = _autentication.GetClient();
            _settingsViewModel = settingsViewModel;
            _spreadsheetViewModel = spreadsheetViewModel;
            _appSettings = _settingsViewModel.GetSettings;
            _players = new List<Player>();
            _participants = new List<Participant>();
            playerCache = new Dictionary<string, List<Player>>();
            Players = new List<Player>();
        }

        public async Task AddPlayer(Player player)
        {
            if (_client == null)
            {
                return;
            }
            string currentTournament = _appSettings.CurrentTournamentDetails.tournamentUrl;
            string currentSHeet = _appSettings.CurrentTournamentDetails.relatedSheetName;
            try
            {

                ParticipantInfo participantInfo = new ParticipantInfo
                {
                    Name = player.Name,
                    Email = player.Email
                };
                var participant = await _client.CreateParticipantAsync(currentTournament, participantInfo);

                player.ChallongeId = participant.Id;
                _players.Add(player);
                playerCache[currentTournament].Add(player);
                await _spreadsheetViewModel.AddNewPlayer(currentSHeet, player);
                OnStateChanged?.Invoke();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public async Task RemovePlayer(Player player)
        {
            if (_client == null)
            {
                return;
            }
            string currentTournament = _appSettings.CurrentTournamentDetails.tournamentUrl;
            string currentSHeet = _appSettings.CurrentTournamentDetails.relatedSheetName;
            try
            {
                // TODO: Remove player from challonge and google sheet
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public async Task GetParticipentsViaURL(string code)
        {
            Console.WriteLine("In GetParticipentsViaURL");
            if (_client == null)
            {
                return;
            }

            if (code != "" && _client != null)
            {
                try
                {
                    Console.WriteLine(code);
                    Console.WriteLine("In Try of GetParticipentsViaURL");
                    if (playerCache.ContainsKey(code))
                    {
                        var cachedPlayers = playerCache[code];

                        bool areEqual = _players.SequenceEqual(cachedPlayers);

                        if (!areEqual)
                        {
                            _players.Clear();
                            _players.AddRange(cachedPlayers);
                        }

                    }
                    else
                    {

                        Console.WriteLine("In Else of GetParticipentsViaURL");
                        var participants = await _client.GetParticipantsAsync(code);

                        Console.WriteLine("Got Participants");
                        List<Participant> participantsList = participants.ToList();

                        ClearPlayers();
                        await AddPlayerFromParticipant(participantsList);

                        // Create a new list instance to avoid reference issues
                        var playersCopy = new List<Player>(_players);
                        playerCache[code] = playersCopy;
                    }


                    var currentSettings = _settingsViewModel.GetSettings;
                    currentSettings.CurrentTournamentDetails.tournamentUrl = code;
                    _settingsViewModel.SaveSettings(currentSettings);
                    OnStateChanged?.Invoke();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + "Error in GetParticipentsViaURL At" + e.StackTrace);
                }

            }
        }

        public async Task AddPlayerFromParticipant(List<Participant> participants)
        {
            var tournament = await _client.GetTournamentByUrlAsync(_appSettings.CurrentTournamentDetails.tournamentUrl);
            var matches = await _client.GetMatchesAsync(tournament);
            _appSettings = _settingsViewModel.GetSettings;
            isLoading = true;
            OnStateChanged?.Invoke();
            List<Player> tempList = new List<Player>();
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
                        else if(match.State == MatchState.Open)
                        {
                            p.Losses++;
                        }
                    }
                }


                _spreadsheetViewModel.AddNewPlayer(_appSettings.CurrentTournamentDetails.relatedSheetName, p);
                _players.Add(p);
            }
            isLoading = false;
            OnStateChanged?.Invoke();
        }

        public async Task OnCheckInStateChanged(Player context)
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                var participant = await _client.GetParticipantAsync(_appSettings.CurrentTournamentDetails.tournamentUrl, context.ChallongeId);

                // Update the player list
                var index = _players.FindIndex(x => x.ChallongeId == context.ChallongeId);

                // if the check in true update time
                if (!context.CheckInState)
                {

                    _players[index].CheckInState = true;
                    _players[index].CheckInTime = DateTime.Now;
                    await _client.CheckInParticipantAsync(participant);
                }
                else
                {
                    _players[index].CheckInState = false;
                    _players[index].CheckInTime = null;

                    await _client.UndoCheckInParticipantAsync(participant);
                }
                OnStateChanged?.Invoke();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        public bool HandleCheckIn(bool isCheckedIn)
        {
            OnStateChanged?.Invoke();
            return isCheckedIn;
        }

        public void SetPlayers(IEnumerable<Player> players)
        {
            _players = players.ToList();
            OnStateChanged?.Invoke();
        }

        public void ClearPlayers()
        {
            _players.Clear();
            OnStateChanged?.Invoke();
        }

        public bool HandleCheckInState(bool isCheckedIn)
        {
            OnStateChanged?.Invoke();
            return isCheckedIn;
        }
        public List<string> Regions
        {
            get
            {
                return new List<string>{"London", "Norwich", "Oxfordshire", "Yorkshire", "South West", "South East",
                                        "North West", "North East", "Midlands", "Wales", "Scotland", "Ireland"};
            }
        }
        private List<Player> _players { get; set; }
        private List<Participant> _participants { get; set; }
        public List<Player> Players
        {
            get
            {
                return _players;
            }
            set
            {
                _players = value;
                OnStateChanged?.Invoke();
            }
        }

        public Dictionary<string, List<Player>> PlayerCache
        {
            get
            {
                OnStateChanged?.Invoke();
                return playerCache;
            }
            set
            {
                playerCache = value;
            }
        }

        public bool isLoading
        {
            get
            {
                return _isLoading;
            }
            set
            {
                _isLoading = value;
                OnStateChanged?.Invoke();
            }
        }
        protected void NotifyStateChanged() => OnStateChanged?.Invoke();

    }
}
using System.Text.RegularExpressions;
using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using BeybladeTournamentManager.ApiCalls.Google;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace BeybladeTournamentManager.Helpers
{
    public class PlayerHelper : IPlayerHelper
    {
        private static List<Player> _currentPlayers;
        private static IGoogleService _googleService;
        private static SpreadsheetsResource.ValuesResource.GetRequest BeybladeSheet;
        private static List<Sheet> _sheets = new List<Sheet>();
        public PlayerHelper(IGoogleService googleService)
        {
            _googleService = googleService;
            _currentPlayers = new List<Player>();
            _sheets = (List<Sheet>)GetSheets();
        }

        public List<Player> GetCurrentPlayers()
        {
            if (_currentPlayers != null && _currentPlayers.Count > 0)
            {
                return _currentPlayers;
            }
            else
            {
                return new List<Player>();
            }
        }

        public Player GetPlayerById(long id)
        {
            return _currentPlayers.FirstOrDefault(p => p.ChallongeId == id);
        }

        public Player GetPlayerByName(string name)
        {
            return _currentPlayers.FirstOrDefault(p => p.Name == name);
        }

        public void UpdatePlayer(Player player)
        {
            var index = _currentPlayers.FindIndex(p => p.ChallongeId == player.ChallongeId);
            _currentPlayers[index] = player;
        }

        public void SetCurrentPlayers(List<Player> players)
        {
            _currentPlayers = players;
        }

        public void AddPlayer(Player player, string sheetName)
        {
            _currentPlayers.Add(player);
            GetPlayerLeaderboardInfo(player, sheetName);
        }

        public void RemovePlayer(Player player)
        {
            _currentPlayers.Remove(player);
        }

        public void ClearPlayers()
        {
            _currentPlayers.Clear();
        }

        public async Task GetPlayerLeaderboardInfo(Player player, string sheetTitle)
        {
            BeybladeSheet = null;

            await SetupSheet(sheetTitle);


            var response = BeybladeSheet.Execute();
            var values = response.Values;

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row.Count > 0 && row[1].ToString().Equals(player.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // Player found, retrieve their data
                        player.LeaderboardRank = row[0].ToString();
                        player.Points = int.Parse(row[2].ToString());
                        string pattern = @"(\d+)/(\d+)";
                        Regex regex = new Regex(pattern);
                        Match match = regex.Match(row[3].ToString());

                        if (match.Success)
                        {
                            player.Wins = int.Parse(match.Groups[1].Value);
                            player.Losses = int.Parse(match.Groups[2].Value);
                        }
                        else
                        {
                            player.Wins = 0;
                            player.Losses = 0;
                        }

                        player.first = int.Parse(row[4].ToString());
                        player.second = int.Parse(row[5].ToString());
                        player.third = int.Parse(row[6].ToString());
                        player.region = row[7].ToString();
                        return;
                    }
                }
            }

            Console.WriteLine($"Player {player.Name} not found.");
        }

        public async Task<List<Player>> GetLeaderboard(string sheetTitle)
        {
            BeybladeSheet = null;
            await SetupSheet(sheetTitle);

            var response = BeybladeSheet.Execute();
            var values = response.Values;
            List<Player> players = new List<Player>();
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row.Count > 0)
                    {
                        Player player = new Player
                        {
                            LeaderboardRank = row.Count > 0 ? row[0].ToString() : string.Empty,
                            Name = row.Count > 1 ? row[1].ToString() : string.Empty,
                            Points = row.Count > 2 ? SafeParseInt(row[2].ToString()) : 0,
                            first = row.Count > 4 ? SafeParseInt(row[4].ToString()) : 0,
                            second = row.Count > 5 ? SafeParseInt(row[5].ToString()) : 0,
                            third = row.Count > 6 ? SafeParseInt(row[6].ToString()) : 0,
                            region = row.Count > 7 ? row[7].ToString() : string.Empty
                        };

                        // Regex pattern to extract wins and losses
                        string pattern = @"(\d+)/(\d+)";
                        Regex regex = new Regex(pattern);
                        Match match = regex.Match(row.Count > 3 ? row[3].ToString() : string.Empty);

                        if (match.Success)
                        {
                            player.Wins = int.Parse(match.Groups[1].Value);
                            player.Losses = int.Parse(match.Groups[2].Value);
                        }
                        else
                        {
                            player.Wins = 0;
                            player.Losses = 0;
                        }

                        players.Add(player);
                    }
                }
            }
            return players;
        }

        private async Task SetupSheet(string sheetTitle)
        {

            // Get the sheet ID from the sheets list
            var sheetId = _sheets.FirstOrDefault(s => s.Properties.Title == sheetTitle)?.Properties.SheetId;
            if (sheetId == null)
            {
                throw new Exception($"Sheet {sheetTitle} not found.");
            }
            else
            {
                BeybladeSheet = _googleService.GetService().Spreadsheets.Values.Get("17vbW07-DwltCwamcXXUq1OgIqg4zsRbWtnN9UX5arQ0", sheetTitle + "!A18:H");
            }
        }
        

        public IList<Sheet> GetSheets()
        {
            var service = _googleService.GetService();

            // Define request parameters.
            SpreadsheetsResource.GetRequest request = service.Spreadsheets.Get("17vbW07-DwltCwamcXXUq1OgIqg4zsRbWtnN9UX5arQ0");

            // Fetch the spreadsheet metadata.
            Spreadsheet spreadsheet = request.Execute();

            // Get the list of sheets.
            IList<Sheet> sheets = spreadsheet.Sheets;
            return sheets;
        }

        public List<Sheet> GetSheetsList()
        {
            if(_sheets.Count == 0)
            {
                _sheets = (List<Sheet>)GetSheets();
            }
            Console.WriteLine(_sheets.Count);
            return _sheets;
        }

        private int SafeParseInt(string value, int defaultValue = 0)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
        }
    }

}
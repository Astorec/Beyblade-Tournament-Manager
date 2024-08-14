using System.Text.RegularExpressions;
using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using BeybladeTournamentManager.ApiCalls.Google;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.VisualBasic;

namespace BeybladeTournamentManager.Helpers
{
    public class PlayerHelper : IPlayerHelper
    {
        private static List<Player> _currentPlayers;
        private static IGoogleServiceFactory _googleServiceFactory;
        private static SpreadsheetsResource.ValuesResource.GetRequest BeybladeSheet;
        private static List<string> _sheets = new List<string>();
        private static Dictionary<string, Sheet> _sheetsCache = new Dictionary<string, Sheet>();
        private readonly Dictionary<string, IList<IList<object>>> _cache = new Dictionary<string, IList<IList<object>>>();
        public PlayerHelper(IGoogleServiceFactory googleServiceFactory)
        {
            _googleServiceFactory = googleServiceFactory;
            _currentPlayers = new List<Player>();
            GetSheets().Wait();
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
        }

        public void RemovePlayer(Player player)
        {
            _currentPlayers.Remove(player);
        }

        public void ClearPlayers()
        {
            _currentPlayers.Clear();
        }


        public async Task GetSheets()
        {
            try
            {
                var _googleService = _googleServiceFactory.Create();
                Console.WriteLine("Getting sheets");
                // Define request parameters.
                SpreadsheetsResource.GetRequest request = _googleService.GetService().Spreadsheets.Get("17vbW07-DwltCwamcXXUq1OgIqg4zsRbWtnN9UX5arQ0");

                // Fetch the spreadsheet metadata.
                Spreadsheet spreadsheet = request.Execute();
                Console.WriteLine("Request Executed");

                Console.WriteLine("Processing Sheets");
                foreach (var sheet in spreadsheet.Sheets)
                {
                    var sheetTitle = sheet.Properties.Title;
                    Console.WriteLine($"Processing sheet {sheetTitle}");

                    var expectedColumnNames = new List<string> { "Rank", "Blader", "Points", "W/L", "1st", "2nd", "3rd", "Region" };
                    var headerRowIndex = -1;

                    // Fetch the sheet data
                    var dataRange = $"{sheetTitle}!A1:Z";
                    var getRequest = _googleService.GetService().Spreadsheets.Values.Get("17vbW07-DwltCwamcXXUq1OgIqg4zsRbWtnN9UX5arQ0", dataRange);
                    ValueRange response = getRequest.Execute();

                    if (response.Values == null || response.Values.Count == 0)
                    {
                        Console.WriteLine($"No data found in sheet {sheetTitle}");
                        continue;
                    }

                    bool found = false;
                    // Find the header row
                    for (int i = 0; i < response.Values.Count; i++)
                    {
                        var row = response.Values[i];
                        if (row.Count >= expectedColumnNames.Count && row.SequenceEqual(expectedColumnNames))
                        {
                            found = true;
                            headerRowIndex = i;
                            break;
                        }
                    }

                    if (!found)
                    {
                        expectedColumnNames.Remove("Region");
                        for (int i = 0; i < response.Values.Count; i++)
                        {
                            var row = response.Values[i];
                            if (row.Count >= expectedColumnNames.Count && row.SequenceEqual(expectedColumnNames))
                            {
                                headerRowIndex = i;
                                break;
                            }
                        }
                    }

                    if (headerRowIndex == -1)
                    {
                        Console.WriteLine($"Header row not found in sheet {sheetTitle}");
                        continue;
                    }

                    // Define the data range based on the header row index
                    var dataStartRow = headerRowIndex + 2; // Assuming data starts two rows after the header
                    var dataRangeToFetch = $"{sheetTitle}!A{dataStartRow}:H";
                    var dataRequest = _googleService.GetService().Spreadsheets.Values.Get("17vbW07-DwltCwamcXXUq1OgIqg4zsRbWtnN9UX5arQ0", dataRangeToFetch);
                    ValueRange dataResponse = dataRequest.Execute();

                    if (dataResponse.Values == null || dataResponse.Values.Count == 0)
                    {
                        Console.WriteLine($"No data found in range {dataRangeToFetch}");
                        continue;
                    }

                    // Save to cache
                    _cache[sheetTitle] = dataResponse.Values;
                    Console.WriteLine($"Data cached for sheet {sheetTitle}");
                }
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine("Request timed out.");
                throw new Exception("The request timed out.", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }

        public IList<IList<object>> GetCachedData(string sheetTitle)
        {
            if (_cache.TryGetValue(sheetTitle, out var data))
            {
                return data;
            }
            else
            {
                Console.WriteLine($"No cached data found for sheet {sheetTitle}");
                return null;
            }
        }

        public async Task<List<Player>> GetLeaderboard(string sheetTitle)
        {
            // Ensure the sheets are set up and data is cached
            if (!_cache.ContainsKey(sheetTitle))
            {
                await GetSheets();
            }

            var values = GetCachedData(sheetTitle);
            List<Player> players = new List<Player>();

            if (values != null && values.Count > 0)
            {

                foreach (var row in values)
                {
                    Console.WriteLine($"Sheet: {sheetTitle} - Row Count: {row.Count()} - Row: {string.Join(", ", row)}");
                    if (row.Count() < 8) // Ensure there are at least 8 elements in the row
                    {
                        Player tmpPlayer = new Player
                        {
                            LeaderboardRank = row[0].ToString(),
                            Name = row[1].ToString(),
                            Points = int.TryParse(row.ElementAtOrDefault(2)?.ToString(), out var points) ? points : 0,
                            first = int.TryParse(row.ElementAtOrDefault(4)?.ToString(), out var first) ? first : 0,
                            second = int.TryParse(row.ElementAtOrDefault(5)?.ToString(), out var second) ? second : 0,
                            third = int.TryParse(row.ElementAtOrDefault(6)?.ToString(), out var third) ? third : 0,
                            region = row.ElementAtOrDefault(7)?.ToString() ?? "N/A",
                        };
                        players.Add(tmpPlayer);
                    }
                    else
                    {
                        Player player = new Player
                        {
                            LeaderboardRank = row[0].ToString(),
                            Name = row[1].ToString(),
                            Points = int.TryParse(row.ElementAtOrDefault(2)?.ToString(), out var points) ? points : 0,
                            first = int.TryParse(row.ElementAtOrDefault(4)?.ToString(), out var first) ? first : 0,
                            second = int.TryParse(row.ElementAtOrDefault(5)?.ToString(), out var second) ? second : 0,
                            third = int.TryParse(row.ElementAtOrDefault(6)?.ToString(), out var third) ? third : 0,
                            region = row.ElementAtOrDefault(7)?.ToString() ?? "N/A",
                        };

                        string pattern = @"(\d+)/(\d+)";
                        Regex regex = new Regex(pattern);
                        string row3Value = row.ElementAtOrDefault(3)?.ToString() ?? string.Empty;
                        Match match = regex.Match(row3Value);

                        if (match.Success)
                        {
                            player.Wins = int.TryParse(match.Groups[1].Value, out var wins) ? wins : 0;
                            player.Losses = int.TryParse(match.Groups[2].Value, out var losses) ? losses : 0;
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
            else
            {
                Console.WriteLine($"No data found for sheet {sheetTitle}");
            }

            return players;
        }

        public List<string> GetSheetsList()
        {
            if (_sheets.Count == 0)
            {
                foreach (var sheet in _cache)
                {
                    _sheets.Add(sheet.Key);
                }
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
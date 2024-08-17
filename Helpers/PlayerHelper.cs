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
            // Get player details from the spreadsheet
            var players = GetLeaderboard(sheetName).Result;
            player = players.FirstOrDefault(p => p.Name == player.Name);

            _currentPlayers.Add(player);
        }

        public void AddPlayers(List<Player> players, string sheetName)
        {
            // Get player details from the spreadsheet
            var updatedPlayers = GetLeaderboard(sheetName).Result;
            foreach (var player in players)
            {
                var updatedPlayer = updatedPlayers.FirstOrDefault(p => p.Name == player.Name);
                if (updatedPlayer != null)
                {
                    _currentPlayers.Add(updatedPlayer);
                }
            }
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

                    // only do this if the sheet is not already in the cache
                    if (!_cache.ContainsKey(sheet.Properties.Title))
                    {
                        bool requiresUpdate = false;


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

                        // Compare the cached data sequance with the fetched data
                        if (_cache.TryGetValue(sheetTitle, out var cachedData))
                        {
                            if (cachedData.SequenceEqual(dataResponse.Values))
                            {
                                Console.WriteLine($"Data in sheet {sheetTitle} is up to date");
                                continue;
                            }
                            else
                            {
                                requiresUpdate = true;
                            }
                        }


                        if (dataResponse.Values == null || dataResponse.Values.Count == 0)
                        {
                            Console.WriteLine($"No data found in range {dataRangeToFetch}");
                            continue;
                        }

                        // Save to cache
                        if (requiresUpdate || !_cache.ContainsKey(sheetTitle))
                            _cache[sheetTitle] = dataResponse.Values;

                        Console.WriteLine($"Data cached for sheet {sheetTitle}");
                    }



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
                Console.WriteLine($"No sheets have been found. Generating new Sheet");
                CreateSheet(sheetTitle);
                GetSheets();
                _cache.Add(sheetTitle, new List<IList<object>>());
                _cache.TryGetValue(sheetTitle, out var newData);

                return newData;
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
                    if (!_sheets.Contains(sheet.Key))
                        _sheets.Add(sheet.Key);
                }
            }
            Console.WriteLine(_sheets.Count);
            return _sheets;
        }

        public async Task CreateSheet(string sheetName)
        {
            try
            {
                var _googleFactory = _googleServiceFactory.Create();
                var _googleService = _googleFactory.GetService();

                // Create a new Sheet Request to google API
                var addSheetRequest = new AddSheetRequest();

                // We then create a new properties object for the sheet
                addSheetRequest.Properties = new SheetProperties();
                addSheetRequest.Properties.Title = sheetName;

                // Once created we then create a new Update Request to send to google API
                BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
                batchUpdateSpreadsheetRequest.Requests = new List<Request>();
                batchUpdateSpreadsheetRequest.Requests.Add(new Request
                {
                    AddSheet = addSheetRequest
                });

                // Send the request to google API
                var batchUpdateRequest = _googleService.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, "17vbW07-DwltCwamcXXUq1OgIqg4zsRbWtnN9UX5arQ0");
                batchUpdateRequest.Execute();


                // Now create a range badsed on the sheet name going from A1 - H1 that setups the column names we require
                var range = $"{sheetName}!A1:H1"; // Adjust the range as needed 
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>> { new List<object> { "Rank", "Blader", "Points", "W/L", "1st", "2nd", "3rd", "Region" } }
                };

                // Create the google request
                var appendRequest = _googleService.Spreadsheets.Values.Update(valueRange, "17vbW07-DwltCwamcXXUq1OgIqg4zsRbWtnN9UX5arQ0", range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

                // Execute the request
                var appendResponse = appendRequest.Execute();
                Console.WriteLine("Data updated in range: " + range);
            }
            catch (Google.GoogleApiException e)
            {
                Console.WriteLine($"Google API Error: {e.Message}");
                Console.WriteLine($"HttpStatusCode: {e.HttpStatusCode}");
                foreach (var error in e.Error.Errors)
                {
                    Console.WriteLine($"Error Message: {error.Message}");
                    Console.WriteLine($"Reason: {error.Reason}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"General Error: {e.Message}");
            }
        }

        public async Task AddPlayersToSheet(string sheetName, List<Player> players)
        {
            var _googleFactory = _googleServiceFactory.Create();
            var _googleService = _googleFactory.GetService();

            // get sheet from cache
            Sheet sheet = GetSheetFromCache(sheetName);

            if (sheet == null)
            {
                throw new Exception($"Sheet {sheetName} not found");
            }

            int count = 2;
            var range = $"{sheetName}!A2:H";
            var values = GetCachedData(sheetName);
            // add values to the sheet
            foreach (var player in players)
            {
                values.Add(new List<object> { player.LeaderboardRank, player.Name, player.Points, $"{player.Wins}/{player.Losses}", player.first, player.second, player.third, player.region });
                count++;
            }


            var valueRange = new ValueRange
            {
                Values = values
            };

            var appendRequest = _googleService.Spreadsheets.Values.Update(valueRange, "17vbW07-DwltCwamcXXUq1OgIqg4zsRbWtnN9UX5arQ0", range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.Execute();

            // update cache
            _cache[sheetName] = values;
        }

        public async Task AddNewPlayer(string sheetName, Player player)
        {
            var _googleFactory = _googleServiceFactory.Create();
            var _googleService = _googleFactory.GetService();

            // get sheet from cache
            Sheet sheet = GetSheetFromCache(sheetName);

            if (sheet == null)
            {
                throw new Exception($"Sheet {sheetName} not found");
            }

            var range = $"{sheetName}!A2:H";

            var values = GetCachedData(sheetName);

            values.Add(new List<object> { player.LeaderboardRank, player.Name, player.Points, $"{player.Wins}/{player.Losses}", player.first, player.second, player.third, player.region });

            var valueRange = new ValueRange
            {
                Values = values
            };

            var appendRequest = _googleService.Spreadsheets.Values.Update(valueRange, "17vbW07-DwltCwamcXXUq1OgIqg4zsRbWtnN9UX5arQ0", range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.Execute();
        }

        public async Task UpdatePlayerInSheet(string sheetName, List<Player> player)
        {
            var _googleFactory = _googleServiceFactory.Create();
            var _googleService = _googleFactory.GetService();

            // get sheet from cache
            Sheet sheet = GetSheetFromCache(sheetName);

            if (sheet == null)
            {
                throw new Exception($"Sheet {sheetName} not found");
            }

            var range = $"{sheetName}!A2:H";

            var values = GetCachedData(sheetName);

            foreach (var p in player)
            {
                var index = values.Select((x, i) => x[1].ToString() == p.Name ? i : -1).FirstOrDefault();

                values[index] = new List<object> { p.LeaderboardRank, p.Name, p.Points, $"{p.Wins}/{p.Losses}", p.first, p.second, p.third, p.region };

            }

            var valueRange = new ValueRange
            {
                Values = values
            };

            var appendRequest = _googleService.Spreadsheets.Values.Update(valueRange, "17vbW07-DwltCwamcXXUq1OgIqg4zsRbWtnN9UX5arQ0", range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.Execute();
        }

        private Sheet GetSheetFromCache(string sheetName)
        {
            var _googleFactory = _googleServiceFactory.Create();
            var _googleService = _googleFactory.GetService();
            if (!_sheetsCache.TryGetValue(sheetName, out var sheet))
            {
                var request = _googleService.Spreadsheets.Get("17vbW07-DwltCwamcXXUq1OgIqg4zsRbWtnN9UX5arQ0");
                var response = request.Execute();
                foreach (var s in response.Sheets)
                {
                    if (s.Properties.Title == sheetName)
                    {
                        _sheetsCache[sheetName] = s;
                        sheet = s;
                        break;
                    }
                }
            }

            return sheet;
        }
        private int SafeParseInt(string value, int defaultValue = 0)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
        }
    }

}
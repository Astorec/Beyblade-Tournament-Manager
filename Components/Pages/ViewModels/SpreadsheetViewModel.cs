
using System.Text.RegularExpressions;
using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using BeybladeTournamentManager.ApiCalls.Google;
using BeybladeTournamentManager.Config;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using NuGet.Configuration;

namespace BeybladeTournamentManager.Components.Pages.ViewModels
{
    public class SpreadsheetViewModel : ISpreadsheetViewModel
    {
        private static IGoogleServiceFactory _googleServiceFactory;
        private static IGoogleService _googleService;
        private readonly Dictionary<string, IList<IList<object>>> _cache = new Dictionary<string, IList<IList<object>>>();
        private static Dictionary<string, Sheet> _sheetsCache = new Dictionary<string, Sheet>();
        private AppSettings _settings;
        string sheetId;
        string currentSheetId;
        public SpreadsheetViewModel(IGoogleServiceFactory googleServiceFactory, ISettingsViewModel settingsViewModel)
        {
            _googleServiceFactory = googleServiceFactory;
            _googleService = _googleServiceFactory.Create();
            _settings = settingsViewModel.GetSettings;
            sheetId = _settings.SheetID;
        }


        public async Task GetSheets()
        {
            try
            {
                Spreadsheet spreadsheet = GetSpreadsheets().Result.Execute();

                foreach (var sheet in spreadsheet.Sheets)
                {

                    // only do this if the sheet is not already in the cache
                    if (!_cache.ContainsKey(sheet.Properties.Title))
                    {
                        var expectedColumnNames = new List<string> { "Rank", "Blader", "Points", "W/L", "1st", "2nd", "3rd", "Region" };
                        bool requiresUpdate = false;
                        var sheetTitle = sheet.Properties.Title;

                        // Get the row header index
                        int headerRowIndex = GetRowHeaderIndex(sheetTitle, expectedColumnNames);

                        if (headerRowIndex == -1)
                        {
                            Console.WriteLine($"Header row not found in sheet {sheetTitle}");
                            continue;
                        }

                        if (headerRowIndex == 6)
                        {
                            expectedColumnNames.Remove("Region");
                        }

                        // See if we need to cache the data again
                        await TryCacheData(sheetTitle, headerRowIndex);
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

        public Sheet GetSheetFromCache(string sheetTitle)
        {
            if (!_sheetsCache.TryGetValue(sheetTitle, out var sheetData))
            {
                var request = GetSpreadsheets().Result.Execute();
                foreach (var s in request.Sheets)
                {
                    if (s.Properties.Title == sheetTitle)
                    {
                        _sheetsCache[sheetTitle] = s;
                        break;
                    }
                }
            }

            return sheetData;
        }

        public async Task CreateSheet(string sheetTitle)
        {
            try
            {
                // Create a new Sheet Request to google API
                var addSheetRequest = new AddSheetRequest();

                // We then create a new properties object for the sheet
                addSheetRequest.Properties = new SheetProperties();
                addSheetRequest.Properties.Title = sheetTitle;

                // Once created we then create a new Update Request to send to google API
                BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
                batchUpdateSpreadsheetRequest.Requests = new List<Request>();
                batchUpdateSpreadsheetRequest.Requests.Add(new Request
                {
                    AddSheet = addSheetRequest
                });

                // Send the request to google API
                var batchUpdateRequest = BatchUpdateSpreadsheet(batchUpdateSpreadsheetRequest).Result.Execute();


                // Now create a range badsed on the sheet name going from A1 - H1 that setups the column names we require
                var range = $"{sheetTitle}!A1:H1"; // Adjust the range as needed 
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>> { new List<object> { "Rank", "Blader", "Points", "W/L", "1st", "2nd", "3rd", "Region" } }
                };

                // Create the google request
                var appendRequest = UpdateSheet(range, valueRange).Result;
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

                // Execute the request
                appendRequest.Execute();
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
                        Player tmpPlayer = AddPlayerFromRows(row);
                        players.Add(tmpPlayer);
                    }
                    else
                    {
                        Player player = AddPlayerFromRows(row);

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
        public async Task AddNewPlayer(string sheetTitle, Player player)
        {
            // Check to see if a sheet exists in the cache, if not create it
            if (!_cache.ContainsKey(sheetTitle))
            {
                await CreateSheet(sheetTitle);
            }

            // get sheet from cache
            Sheet sheet = GetSheetFromCache(sheetTitle);

            if (sheet == null)
            {
                throw new Exception($"Sheet {sheetTitle} not found");
            }

            var range = $"{sheetTitle}!A2:H";

            var values = GetSheetValues(sheetTitle).Result.Execute().Values;

            values.Add(new List<object> { player.LeaderboardRank, player.Name, player.Points, $"{player.Wins}/{player.Losses}", player.first, player.second, player.third, player.region });

            var valueRange = new ValueRange
            {
                Values = values
            };

            var appendRequest = UpdateSheet(range, valueRange).Result;
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.Execute();
        }

        public async Task UpdatePlayers(string sheetTtile, List<Player> players)
        {
            // get sheet from cache
            Sheet sheet = GetSheetFromCache(sheetTtile);

            if (sheet == null)
            {
                throw new Exception($"Sheet {sheetTtile} not found");
            }

            var range = $"{sheetTtile}!A2:H";

            var values = GetCachedData(sheetTtile);

            foreach (var p in players)
            {
                var index = values.Select((x, i) => x[1].ToString() == p.Name ? i : -1).FirstOrDefault();

                values[index] = new List<object> { p.LeaderboardRank, p.Name, p.Points, $"{p.Wins}/{p.Losses}", p.first, p.second, p.third, p.region };

            }

            var valueRange = new ValueRange
            {
                Values = values
            };

            var appendRequest = UpdateSheet(range, valueRange).Result;
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.Execute();
        }

        /// <summary>
        /// Get the sheets from Google Sheets
        /// </summary>
        /// <param name="sheetId"></param>
        /// <returns></returns>
        private async Task<SpreadsheetsResource.GetRequest> GetSpreadsheets()
        {
            return _googleService.GetService().Spreadsheets.Get(sheetId);
        }

        private async Task<SpreadsheetsResource.ValuesResource.GetRequest> GetSheetValues(string range)
        {
            return _googleService.GetService().Spreadsheets.Values.Get(sheetId, range);
        }
        /// <summary>
        /// Create a new sheet in the spreadsheet
        /// </summary>
        /// <param name="sheetId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<SpreadsheetsResource.BatchUpdateRequest> BatchUpdateSpreadsheet(BatchUpdateSpreadsheetRequest request)
        {
            return _googleService.GetService().Spreadsheets.BatchUpdate(request, sheetId);
        }

        /// <summary>
        /// Update the sheet with the new data
        /// </summary>
        /// <param name="range"></param>
        /// <param name="valueRange"></param>
        /// <returns></returns>
        private async Task<SpreadsheetsResource.ValuesResource.UpdateRequest> UpdateSheet(string range, ValueRange valueRange)
        {
            return _googleService.GetService().Spreadsheets.Values.Update(valueRange, sheetId, range);
        }

        /// <summary>
        /// Get the row index of the header row in the sheet
        /// </summary>
        /// <param name="sheetTitle">The title of the sheet</param>
        /// <param name="expectedColumnNames">Columns that should be present on the sheet</param>
        /// <returns></returns>
        private int GetRowHeaderIndex(string sheetTitle, List<string> expectedColumnNames)
        {
            var headerRowIndex = -1;

            // Fetch the sheet data
            var dataRange = $"{sheetTitle}!A1:Z";
            var getRequest = _googleService.GetService().Spreadsheets.Values.Get(sheetId, dataRange);
            ValueRange response = getRequest.Execute();

            if (response.Values == null || response.Values.Count == 0)
            {
                Console.WriteLine($"No data found in sheet {sheetTitle}");
            }

            // Find the header row
            for (int i = 0; i < response.Values.Count; i++)
            {
                var row = response.Values[i];
                if (row.Count >= expectedColumnNames.Count && row.SequenceEqual(expectedColumnNames))
                {
                    headerRowIndex = i;
                    break;
                }
                else if (row.Count >= expectedColumnNames.Count - 1 && !row.SequenceEqual(expectedColumnNames))
                {
                    headerRowIndex = i;
                    break;
                }
            }
            return headerRowIndex;
        }

        /// <summary>
        /// Try to cache the data from the sheet and update it where required.
        /// </summary>
        /// <param name="sheetTitle">Sheet Name</param>
        /// <param name="headerRowIndex">Row index for start of data</param>
        /// <returns></returns>
        private async Task TryCacheData(string sheetTitle, int headerRowIndex)
        {
            bool requiresUpdate = false;
            // Define the data range based on the header row index
            var dataStartRow = headerRowIndex + 2; // Assuming data starts two rows after the header
            // Set the data range based on the sheet title and then we use the starting row to get the data
            var dataRangeToFetch = $"{sheetTitle}!A{dataStartRow}:H";
            var dataRequest = _googleService.GetService().Spreadsheets.Values.Get("17vbW07-DwltCwamcXXUq1OgIqg4zsRbWtnN9UX5arQ0", dataRangeToFetch);
            ValueRange dataResponse = dataRequest.Execute();

            // Compare the cached data sequance with the fetched data
            if (_cache.TryGetValue(sheetTitle, out var cachedData))
            {
                if (cachedData.SequenceEqual(dataResponse.Values))
                {
                    Console.WriteLine($"Data in sheet {sheetTitle} is up to date");
                }
                else
                {
                    requiresUpdate = true;
                }
            }


            if (dataResponse.Values == null || dataResponse.Values.Count == 0)
            {
                Console.WriteLine($"No data found in range {dataRangeToFetch}");
            }

            // Save to cache
            if (requiresUpdate || !_cache.ContainsKey(sheetTitle))
                _cache[sheetTitle] = dataResponse.Values;
        }

        private string GetSheetId(string sheetTitle)
        {
            return _sheetsCache[sheetTitle].Properties.SheetId.ToString();
        }
        private IList<IList<object>> GetCachedData(string sheetTitle)
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

        /// <summary>
        /// Add player to the list after adding the details. We do this one if it has 7 rows
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private Player AddPlayerFromRows(IList<object> row)
        {
            return new Player
            {
                LeaderboardRank = row[0].ToString(),
                Name = row[1].ToString(),
                Points = int.TryParse(row.ElementAtOrDefault(2)?.ToString(), out var points) ? points : 0,
                first = int.TryParse(row.ElementAtOrDefault(4)?.ToString(), out var first) ? first : 0,
                second = int.TryParse(row.ElementAtOrDefault(5)?.ToString(), out var second) ? second : 0,
                third = int.TryParse(row.ElementAtOrDefault(6)?.ToString(), out var third) ? third : 0,
                region = row.ElementAtOrDefault(7)?.ToString() ?? "N/A",
            };
        }

        // Set the leaderboard to the list of keys in the dictionary
        public List<string> Leaderboard
        {
            get
            {
                return _cache.Keys.ToList();
            }
        }
    }
}
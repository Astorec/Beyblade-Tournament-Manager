
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
        bool isFirstLoad = true;
        public SpreadsheetViewModel(IGoogleService googleService, ISettingsViewModel settingsViewModel)
        {
            _googleService = googleService;
            _settings = settingsViewModel.GetSettings;
            sheetId = _settings.SheetID;

        }

        public async Task GetSheets()
        {
            try
            {
                // Get the latest sheet ID
                sheetId = _settings.SheetID;
                var getSheet = GetSpreadsheets().Result;
                Spreadsheet spreadsheet = getSheet.Execute();
                foreach (var sheet in spreadsheet.Sheets)
                {

                    // only do this if the sheet is not already in the cache
                    if (!_cache.ContainsKey(sheet.Properties.Title))
                    {
                        var expectedColumnNames = new List<string> { "Rank", "Blader", "Wins", "Losses", "1st", "2nd", "3rd",
                        "Points", "Win%", "Rating", "Region", "Column 1", "Column 2", "Column 3", "Column 4" , "Column 5",
                        "Column 6", "Column 7", "Column 8", "Column 9" };
                        bool requiresUpdate = false;
                        var sheetTitle = sheet.Properties.Title;

                        // Get the row header index
                        int headerRowIndex = GetRowHeaderIndex(sheetTitle, expectedColumnNames);

                        if (headerRowIndex == -1)
                        {
                            Console.WriteLine($"Header row not found in sheet {sheetTitle}");
                            continue;
                        }


                        var dataResponse = GetSheetValues($"{sheetTitle}!A{headerRowIndex + 2}:T").Result.Execute();
                        // See if we need to cache the data again
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
                            Console.WriteLine($"No data found in range");
                            // Save to cache with empty data
                            _cache[sheetTitle] = new List<IList<object>>();
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

        public Sheet GetSheetFromCache(string sheetTitle)
        {
            if (_sheetsCache.ContainsKey(sheetTitle))
            {
                return _sheetsCache[sheetTitle];
            }


            // Check to see if we can pull the sheet from google
            var getSheet = GetSpreadsheets().Result;
            Spreadsheet spreadsheet = getSheet.Execute();
            foreach (var sheet in spreadsheet.Sheets)
            {
                if (sheet.Properties.Title == sheetTitle)
                {
                    _sheetsCache.Add(sheetTitle, sheet);
                    return sheet;
                }
            }

            return null;
        }

        public async Task CreateSheet(string sheetTitle)
        {
            try
            {
                // Create a new Sheet Request to google API
                var addSheetRequest = new AddSheetRequest
                {
                    // We then create a new properties object for the sheet
                    Properties = new SheetProperties
                    {
                        Title = sheetTitle
                    }
                };

                // Once created we then create a new Update Request to send to google API
                BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request>()
                };
                batchUpdateSpreadsheetRequest.Requests.Add(new Request
                {
                    AddSheet = addSheetRequest
                });

                // Send the request to google API
                var batchUpdateRequest = BatchUpdateSpreadsheet(batchUpdateSpreadsheetRequest).Result;

                batchUpdateRequest.Execute();

                // Now create a range badsed on the sheet name going from A1 - H1 that setups the column names we require
                var range = $"{sheetTitle}!A1:T1"; // Adjust the range as needed 
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>> { new List<object> { "Rank", "Blader", "Wins", "Losses", "1st", "2nd", "3rd",
                        "Points", "Win%", "Rating", "Region", "Column 1", "Column 2", "Column 3", "Column 4" , "Column 5",
                        "Column 6", "Column 7", "Column 8", "Column 9" } }
                };

                // Create the google request
                var appendRequest = UpdateSheet(range, valueRange).Result;
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

                // Execute the request
                var result = appendRequest.Execute();

                List<string> expectedColumnNames = new List<string>{"Rank", "Blader", "Wins", "Losses", "1st", "2nd", "3rd",
                        "Points", "Win%", "Rating", "Region", "Column 1", "Column 2", "Column 3", "Column 4" , "Column 5",
                        "Column 6", "Column 7", "Column 8", "Column 9" };
                // get header index
                int headerRowIndex = GetRowHeaderIndex(sheetTitle, expectedColumnNames);
                var dataResponse = GetSheetValues($"{sheetTitle}!A{headerRowIndex + 2}:T").Result.Execute();
                _cache[sheetTitle] = dataResponse.Values;
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

                    Player player = AddPlayerFromRows(row);

                    players.Add(player);
                }
            }
            else
            {
                Console.WriteLine($"No data found for sheet {sheetTitle}");
            }

            return players;
        }
        public async Task AddNewPlayers(string sheetTitle, List<Player> players)
        {
            // Don't like adding these in, but found that when the sheet is created and we move on to populating it
            // with the players, the sheet would start overriding data and I think it was using the first row even
            // though it has the headers. I think because the data has had a chance to update yet on the google side,
            // we end up going in to the first row instead of hte next free one. So I've added this to add some kind
            // of delay, it works for now but it is dirty and not ideal.
            Thread.Sleep(1000);
            try
            {
                // Check to see if a sheet exists in the cache, if not create it
                if (!_cache.ContainsKey(sheetTitle))
                {
                    await CreateSheet(sheetTitle);
                }

                // Get sheet from cache
                Sheet sheet = GetSheetFromCache(sheetTitle);

                if (sheet == null)
                {
                    Console.WriteLine($"Sheet {sheetTitle} not found");
                    return;
                }

                var values = GetSheetValues(sheetTitle).Result.Execute().Values.ToList();

                // Find header index
                var expectedColumnNames = new List<string> { "Rank", "Blader", "Wins", "Losses", "1st", "2nd", "3rd",
                        "Points", "Win%", "Rating", "Region", "Column 1", "Column 2", "Column 3", "Column 4" , "Column 5",
                        "Column 6", "Column 7", "Column 8", "Column 9" };
                int headerRowIndex = GetRowHeaderIndex(sheetTitle, expectedColumnNames);
                // Temp List to hold new valueRanges
                List<ValueRange> valueRanges = new List<ValueRange>();
                int count = headerRowIndex + 2;


                foreach (var p in players)
                {
                    // Check to see if the player already exists in the sheet
                    if (values != null)
                    {
                        foreach (var row in values)
                        {
                            if (row[1].ToString() == p.Name)
                            {
                                Console.WriteLine($"Player {p.Name} already exists in the sheet {sheetTitle}");
                                break;
                            }
                        }

                        var emptyRow = count;
                        string rank = $"=RANK(J{emptyRow},J${emptyRow}:J$400,0)";
                        var range = $"{sheetTitle}!A{emptyRow}:T";
                        string sumFormula = $"=SUM(C{emptyRow},(E{emptyRow}*3),(F{emptyRow}*2),(G{emptyRow}*1))";
                        string winRatioFormula = $"=IFERROR(ROUND((C{emptyRow}/(C{emptyRow}+D{emptyRow})), 2), \"\")";
                        string customFormula = $"=ROUND((H{emptyRow} * 100) * I{emptyRow})";
                        // Add the new row with formulas
                        var newRow = new List<object>
                        {
                            rank,
                            p.Name,
                            p.Wins,
                            p.Losses,
                            p.first,
                            p.second,
                            p.third,
                            sumFormula,
                            winRatioFormula,
                            customFormula,
                            p.region
                        };
                        var updateRange = $"{sheetTitle}!A{emptyRow}:T";

                        var valueRange = new ValueRange
                        {
                            Range = updateRange,
                            Values = new List<IList<object>> { newRow }
                        };
                        valueRanges.Add(valueRange);
                        count++;
                    }
                }

                // Update the sheet with the new values
                var batchUpdateRequest = new BatchUpdateValuesRequest
                {
                    Data = valueRanges,
                    ValueInputOption = "USER_ENTERED"
                };

                var batchUpdate = _googleService.GetService().Spreadsheets.Values.BatchUpdate(batchUpdateRequest, sheetId);
                batchUpdate.Execute();

                // Update cache from google not including header
                var dataResponse = GetSheetValues($"{sheetTitle}!A{headerRowIndex + 2}:T").Result.Execute();
                _cache[sheetTitle] = dataResponse.Values;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        public async Task UpdatePlayersInMainSheet(List<Player> players)
        {
            // Get the first sheet from the google sheet api
            var mainSheet = GetMainSheet().Result.Execute();
            bool toBeUpdated = false;
            // Get the values from the sheet
            var values = mainSheet.Values.ToList();
            // Find header index
            var expectedColumnNames = new List<string> { "Rank", "Blader", "Wins", "Losses", "1st", "2nd", "3rd",
                        "Points", "Win%", "Rating", "Region", "Column 1", "Column 2", "Column 3", "Column 4" , "Column 5",
                        "Column 6", "Column 7", "Column 8", "Column 9" };

            // get header index from mainSheet
            int headerRowIndex = mainSheet.Values.ToList().FindIndex(x => x.SequenceEqual(expectedColumnNames));

            List<ValueRange> valueRanges = new List<ValueRange>();
            int count = 1;
            foreach (var p in players)
            {
                bool updatedUser = false;
                // Check to see if the player already exists in the sheet
                if (values != null)
                {
                    foreach (var row in values)
                    {
                        if (values.Any(x => x.Count > 1 && x[1].ToString() == p.Name))
                        {
                            updatedUser = true;
                            // get row to update
                            var index = values.FindIndex(x => x[1].ToString() == p.Name);

                            string updateTank = $"=RANK(J{index},J${index}:J$400,0)";
                            var updatedRange = $"A{index}:T";
                            string updatedSumFormula = $"=SUM(C{index},(E{index}*3),(F{index}*2),(G{index}*1))";
                            string updatedWinRatioFormula = $"=IFERROR(ROUND((C{index}/(C{index}+D{index})), 2), \"\")";
                            string updatedCustomFormula = $"=ROUND((H{index} * 100) * I{index})";
                            var updatedRow = new List<object>
                        {
                            updateTank,
                            p.Name,
                            p.Wins,
                            p.Losses,
                            p.first,
                            p.second,
                            p.third,
                            updatedSumFormula,
                            updatedWinRatioFormula,
                            updatedCustomFormula,
                            p.region
                        };
                            var existingRange = $"A{index}:T";
                            var updatedValueRange = new ValueRange
                            {
                                Range = existingRange,
                                Values = new List<IList<object>> { updatedRow }
                            };
                            valueRanges.Add(updatedValueRange);
                            break;
                        }
                    }

                    if (!updatedUser)
                    {
                        // Get next avaliable row where blader is blank and is after the header
                        var emptyRow = values.FindIndex(x => x.Count > 1 && string.IsNullOrEmpty(x[1].ToString())) + count;

                        string rank = $"=RANK(J{emptyRow},J${emptyRow}:J$400,0)";
                        var range = $"A{emptyRow}:T";
                        string sumFormula = $"=SUM(C{emptyRow},(E{emptyRow}*3),(F{emptyRow}*2),(G{emptyRow}*1))";
                        string winRatioFormula = $"=IFERROR(ROUND((C{emptyRow}/(C{emptyRow}+D{emptyRow})), 2), \"\")";
                        string customFormula = $"=ROUND((H{emptyRow} * 100) * I{emptyRow})";
                        // Add the new row with formulas
                        var newRow = new List<object>
                        {
                            rank,
                            p.Name,
                            p.Wins,
                            p.Losses,
                            p.first,
                            p.second,
                            p.third,
                            sumFormula,
                            winRatioFormula,
                            customFormula,
                            p.region
                        };
                        var updateRange = $"A{emptyRow}:T";
                        var valueRange = new ValueRange
                        {
                            Range = updateRange,
                            Values = new List<IList<object>> { newRow }
                        };
                        valueRanges.Add(valueRange);
                        count++;
                    }
                }
            }

            // add each value in the sheet based on player names
            foreach (var p in players)
            {
                // Find the player in the values IList<IList<object>> and update the values
                var index = valueRanges.FindIndex(x => x.Values.Where(y => y.Select(z => z.ToString()).Contains(p.Name)).Any());

                // convert the values to int
                int wins = int.TryParse(values[index][2].ToString(), out var totalWins) ? totalWins : 0;
                int losses = int.TryParse(values[index][3].ToString(), out var totalLosses) ? totalLosses : 0;
                int first = int.TryParse(values[index][4].ToString(), out var totalFirst) ? totalFirst : 0;
                int second = int.TryParse(values[index][5].ToString(), out var totalSecond) ? totalSecond : 0;
                int third = int.TryParse(values[index][6].ToString(), out var totalThird) ? totalThird : 0;

                // update the values
                wins += p.Wins;
                losses += p.Losses;
                first += p.first;
                second += p.second;
                third += p.third;

                // Update the values
                values[index][2] = wins;
                values[index][3] = losses;
                values[index][4] = first;
                values[index][5] = second;
                values[index][6] = third;
            }

            // update sheet based on valueRanges
            var batchUpdateRequest = new BatchUpdateValuesRequest
            {
                Data = valueRanges,
                ValueInputOption = "USER_ENTERED"
            };

            var batchUpdate = _googleService.GetService().Spreadsheets.Values.BatchUpdate(batchUpdateRequest, sheetId);
            batchUpdate.Execute();
        }

        public async Task UpdatePlayers(string sheetTtile, List<Player> players)
        {
            // get sheet from cache
            Sheet sheet = GetSheetFromCache(sheetTtile);

            if (sheet == null)
            {
                throw new Exception($"Sheet {sheetTtile} not found");
            }

            // Find the range for the sheet based on the column names
            var expectedColumnNames = new List<string> { "Rank", "Blader", "Wins", "Losses", "1st", "2nd", "3rd",
                        "Points", "Win%", "Rating", "Region", "Column 1", "Column 2", "Column 3", "Column 4" , "Column 5",
                        "Column 6", "Column 7", "Column 8", "Column 9" };
            var headerRowIndex = GetRowHeaderIndex(sheetTtile, expectedColumnNames);

            var range = $"{sheetTtile}!A{headerRowIndex + 2}:T";

            var values = GetCachedData(sheetTtile);
            var valuesList = values as List<IList<object>>;

            foreach (var p in players)
            {
                // Find the player in the values IList<IList<object>> and update the values
                var index = valuesList.FindIndex(x => x[1].ToString() == p.Name);
                valuesList[index][0] = $"=RANK(J{index + 2},J${index + 2}:J$400,0)";
                valuesList[index][2] = p.Wins;
                valuesList[index][3] = p.Losses;
                valuesList[index][4] = p.first;
                valuesList[index][5] = p.second;
                valuesList[index][6] = p.third;
                valuesList[index][7] = $"=SUM(C{index + 2},(E{index + 2}*3),(F{index + 2}*2),(G{index + 2}*1))";
                valuesList[index][8] = $"=IFERROR(ROUND((C{index + 2}/(C{index + 2}+D{index + 2})), 2), \"\")";
                valuesList[index][9] = $"=ROUND((H{index + 2} * 100) * I{index + 2})";
            }

            // Only update the row in specific columns
            var valueRange = new ValueRange
            {
                Values = valuesList
            };

            var appendRequest = UpdateSheet(range, valueRange).Result;
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.Execute();

            // update cache not
            _cache[sheetTtile] = valuesList;
        }

        // sort spreadhsheet by rating
        public async Task SortSheet(string sheetTitle)
        {
            // get sheet from cache
            Sheet sheet = GetSheetFromCache(sheetTitle);

            if (sheet == null)
            {
                throw new Exception($"Sheet {sheetTitle} not found");
            }

            // Find the range for the sheet based on the column names
            var expectedColumnNames = new List<string> { "Rank", "Blader", "Wins", "Losses", "1st", "2nd", "3rd",
                        "Points", "Win%", "Rating", "Region", "Column 1", "Column 2", "Column 3", "Column 4" , "Column 5",
                        "Column 6", "Column 7", "Column 8", "Column 9" };
            var headerRowIndex = GetRowHeaderIndex(sheetTitle, expectedColumnNames);

            var range = $"{sheetTitle}!A{headerRowIndex + 2}:T";

            var values = GetCachedData(sheetTitle);
            var valuesList = values as List<IList<object>>;

            var valueRange = new ValueRange
            {
                Values = valuesList
            };

            var appendRequest = UpdateSheet(range, valueRange).Result;
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.Execute();

            // update cache
            _cache[sheetTitle] = valuesList;
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


        private async Task<SpreadsheetsResource.ValuesResource.AppendRequest> AppendSheet(string range, ValueRange valueRange)
        {
            return _googleService.GetService().Spreadsheets.Values.Append(valueRange, sheetId, range);
        }

        private async Task<SpreadsheetsResource.ValuesResource.GetRequest> GetMainSheet()
        {
            // find the first sheet in the list of sheets
            var getSheet = GetSpreadsheets().Result;
            Spreadsheet spreadsheet = getSheet.Execute();

            return _googleService.GetService().Spreadsheets.Values.Get(sheetId, $"{spreadsheet.Sheets[0].Properties.Title}!A1:T");
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
            var dataRangeToFetch = $"{sheetTitle}!A{dataStartRow}:T";
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
                _cache.TryGetValue(sheetTitle, out var retunData);

                return retunData;
            }
        }

        /// <summary>
        /// Add player to the list after adding the details. We do this one if it has 7 rows
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private Player AddPlayerFromRows(IList<object> row)
        {
            Player newPlayer = new Player
            {
                LeaderboardRank = row.ElementAtOrDefault(0)?.ToString() ?? "N/A",
                Name = row.ElementAtOrDefault(1)?.ToString() ?? "N/A",
                Wins = int.TryParse(row.ElementAtOrDefault(2)?.ToString(), out var wins) ? wins : 0,
                Losses = int.TryParse(row.ElementAtOrDefault(3)?.ToString(), out var losses) ? losses : 0,
                first = int.TryParse(row.ElementAtOrDefault(4)?.ToString(), out var first) ? first : 0,
                second = int.TryParse(row.ElementAtOrDefault(5)?.ToString(), out var second) ? second : 0,
                third = int.TryParse(row.ElementAtOrDefault(6)?.ToString(), out var third) ? third : 0,
                Points = int.TryParse(row.ElementAtOrDefault(7)?.ToString(), out var points) ? points : 0,
                WinPercentage = double.TryParse(row.ElementAtOrDefault(8)?.ToString(), out var winPercentage) ? winPercentage : 0,
                Rating = int.TryParse(row.ElementAtOrDefault(9)?.ToString(), out var rating) ? rating : 0,
                region = row.ElementAtOrDefault(10)?.ToString() ?? "N/A"
            };

            return newPlayer;
        }

        // Set the leaderboard to the list of keys in the dictionary
        public List<string> Leaderboard
        {
            get
            {
                if (_cache.Count == 0)
                {
                    GetSheets();
                }
                return _cache.Keys.ToList();
            }
        }
    }
}
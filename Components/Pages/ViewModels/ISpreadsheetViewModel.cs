using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using Google.Apis.Sheets.v4.Data;

namespace BeybladeTournamentManager.Components.Pages.ViewModels
{
    public interface ISpreadsheetViewModel
    {
        /// <summary>
        /// Get the sheets from Google Sheets
        /// </summary>
        /// <param name="sheetId">Sheet ID that is Located in the Google Sheets URL</param>
        /// <returns></returns>
        Task GetSheets();
        Sheet GetSheetFromCache(string sheetTitle);
        Task CreateSheet(string sheetTitle);
        Task AddNewPlayers(string sheetName, List<Player> players);
        Task UpdatePlayersInMainSheet(List<Player> players);
        Task UpdatePlayers(string sheetName, List<Player> players);
        Task<List<Player>> GetLeaderboard(string sheetTitle);
        Task SortSheet(string sheetTitle);
        List<string> Leaderboard { get; }
    }
}
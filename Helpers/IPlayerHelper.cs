using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using Google.Apis.Sheets.v4.Data;

namespace BeybladeTournamentManager.Helpers
{
    public interface IPlayerHelper
    {
        public void AddPlayer(Player player, string sheetName);
        public void AddPlayers(List<Player> players, string sheetName);
        public void RemovePlayer(Player player);
        public void UpdatePlayer(Player player);
        public void SetCurrentPlayers(List<Player> players);
        public List<Player> GetCurrentPlayers();
        public Player GetPlayerById(long id);
        public Player GetPlayerByName(string name);

        public void ClearPlayers();
        public Task<List<Player>> GetLeaderboard(string sheetTitle);
        public Task GetSheets();
        public List<string> GetSheetsList();
        public Task CreateSheet(string sheetName);
        public Task AddPlayersToSheet(string sheetName, List<Player> players);
        public Task AddNewPlayer(string sheetName, Player player);
        public Task UpdatePlayerInSheet(string sheetName, List<Player> player);
    }
}
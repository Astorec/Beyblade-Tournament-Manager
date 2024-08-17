using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using Challonge.Api;
using Challonge.Objects;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public interface ITournamentManager
    {
        public Task<Tournament> GetTournament(string tournamentUrl);
        public Task CreateTournament(TournamentInfo info, bool ignoreNulls = true);
        public TournamentDetails SetTournamentDetails(string url, string tournament, string sheetName);
        public TournamentDetails GetTournamentDetails();
    }
}
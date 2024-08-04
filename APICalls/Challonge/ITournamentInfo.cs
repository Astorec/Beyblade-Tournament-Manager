using Challonge.Api;
using Challonge.Objects;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public interface ITournamentManager
    {
        public Task<Tournament> GetTournament(string tournamentUrl);
        public Task CreateTournament(TournamentInfo info, bool ignoreNulls = true);
    }
}
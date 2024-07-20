using Challonge.Objects;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public interface ITournamentInfo
    {
        public Task<Tournament> GetTournament(string tournamentUrl);
    }
}
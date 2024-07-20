using Challonge.Objects;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public interface IMatches
    {
        public Task<List<Match>> GetMatches(string tournamentUrl);

    }
}
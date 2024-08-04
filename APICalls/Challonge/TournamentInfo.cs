using Challonge.Api;
using Challonge.Objects;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public class TournamentManager : ITournamentManager
    {
        readonly IAutentication _auth;
        readonly ChallongeClient _client;

        public TournamentManager(IAutentication auth)
        {
            _auth = auth;
            _client = _auth.GetClient();
        }

        public Task CreateTournament(TournamentInfo info, bool ignoreNulls = true)
        {
            throw new NotImplementedException();
        }

        public async Task<Tournament> GetTournament(string tournamentUrl)
        {
            return await _client.GetTournamentByUrlAsync(tournamentUrl);
        }
    }
}
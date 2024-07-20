using Challonge.Api;
using Challonge.Objects;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public class Matches : IMatches
    {
        readonly IAutentication _auth;
        readonly ChallongeClient _client;

        public Matches(IAutentication auth)
        {
            _auth = auth;
            _client = _auth.GetClient();
        }
        public async Task<List<Match>> GetMatches(string tournamentUrl)
        {
            return (await _client.GetMatchesAsync(tournamentUrl)).ToList();
        }
    }
}
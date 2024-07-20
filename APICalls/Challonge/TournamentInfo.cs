using Challonge.Api;
using Challonge.Objects;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public class TournamentInfo : ITournamentInfo
    {
        readonly IAutentication _auth;
        readonly ChallongeClient _client;

        public TournamentInfo(IAutentication auth)
        {
            _auth = auth;
            _client = _auth.GetClient();
        }

        public async Task<Tournament> GetTournament(string tournamentUrl)
        {
            return await _client.GetTournamentByUrlAsync(tournamentUrl);
        }
    }
}
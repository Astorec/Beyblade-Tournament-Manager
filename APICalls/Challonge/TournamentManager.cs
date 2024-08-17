using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using Challonge.Api;
using Challonge.Objects;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public class TournamentManager : ITournamentManager
    {
        readonly IAutentication _auth;
        readonly ChallongeClient _client;
        private static TournamentDetails _details;

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

        public TournamentDetails SetTournamentDetails(string url, string tournament, string sheetName)
        {
            _details = new TournamentDetails
            {
                tournamentUrl = url,
                tournamentName = tournament,
                relatedSheetName = sheetName
            };

            return _details;
        }

        public TournamentDetails GetTournamentDetails()
        {
            return _details;
        }
    }
}
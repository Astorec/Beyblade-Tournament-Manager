using BeybladeTournamentManager.Config;
using Challonge.Api;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public interface IAutentication
    {
        public ChallongeClient GetClient();
    }
}
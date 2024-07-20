namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public class TournementInfo
    {
        readonly IAutentication _auth;

        public TournementInfo(IAutentication auth)
        {
            _auth = auth;
        }
    }
}
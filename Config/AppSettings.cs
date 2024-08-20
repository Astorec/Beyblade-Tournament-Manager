using BeybladeTournamentManager.ApiCalls.Challonge.Data;

namespace BeybladeTournamentManager.Config
{
    public class AppSettings
    {
        public string? EncryptionKey { get; set; }
        public string? ChallongeAPIKey { get; set; }
        public string? ChallongeUsername { get; set; }
        public string? GoogleAppName {get; set;}
        public string SheetID {get; set;}
        public Dictionary<string, string>? PreviousTournements{get; set;}
        public string CurrentTournament{get; set;}
        public TournamentDetails? CurrentTournamentDetails {get; set;}
    }
}
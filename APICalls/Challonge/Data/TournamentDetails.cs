
namespace BeybladeTournamentManager.ApiCalls.Challonge.Data
{
    public class TournamentDetails
    {
        public string tournamentUrl { get; set; }
        public string tournamentName { get; set; }
        public string relatedSheetName { get; set; }
        public bool isCompleted {get; set;}
        public bool addedToMainSheet {get; set;}

        public static implicit operator TournamentDetails?(string? v)
        {
            throw new NotImplementedException();
        }
    }
}
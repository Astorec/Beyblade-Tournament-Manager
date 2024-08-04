namespace BeybladeTournamentManager.ApiCalls.Challonge.Data
{
    public class Player
    {
        public string Name { get; set; }
        public string Region {get; set;}
        public bool CheckInState { get; set; }
        public DateTime? CheckInTime {get; set;}
        public long ChallongeId { get; set; }
    }
}
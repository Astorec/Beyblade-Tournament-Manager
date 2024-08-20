namespace BeybladeTournamentManager.ApiCalls.Challonge.Data
{
    public class Player
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public bool CheckInState { get; set; }
        public DateTime? CheckInTime {get; set;}
        public long ChallongeId { get; set; }
        public string LeaderboardRank { get; set; }
        public int Points {get; set;}
        public int Wins {get; set;}
        public int Losses {get; set;}
        public int first {get; set;}
        public int second {get; set;}
        public int third {get; set;}
        public double WinPercentage {get; set;}
        public int Rating {get; set;}
        public string region{get; set;}
    }
}
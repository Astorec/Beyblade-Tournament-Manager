using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using Challonge.Objects;

namespace BeybladeTournamentManager.Components.Pages.ViewModels
{

    public interface IPlayersViewModel
    {
        event Action OnStateChanged;
        Task AddPlayer(Player player);
        //IEnumerable<Player> GetPlayers();
        void SetPlayers(IEnumerable<Player> players);
        void ClearPlayers();
        Task AddPlayerFromParticipant(List<Participant> participants);
        Task RemovePlayer(Player player);
        Task GetParticipentsViaURL(string code);
        Task OnCheckInStateChanged(Player context);
        bool HandleCheckInState(bool isCheckedIn);
        Dictionary<string, List<Player>> PlayerCache { get; set; }

        List<Player> Players { get; set;}
        List<string> Regions { get; }
    }
}
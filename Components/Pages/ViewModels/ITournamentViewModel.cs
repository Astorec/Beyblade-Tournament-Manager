using BeybladeTournamentManager.ApiCalls.Challonge.Data;

namespace BeybladeTournamentManager.Components.Pages.ViewModels
{
    public interface ITournamentViewModel
    {
      //  Task AddTournament(Challonge.Objects.Tournament tournament);
        Task AddedNewTournament(bool added);
        Task HandleUrlAdded(string url);
        TournamentDetails TournamentDetails { get; set; }
    }
}
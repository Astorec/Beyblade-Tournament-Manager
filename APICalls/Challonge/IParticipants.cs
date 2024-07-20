using Challonge.Objects;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public interface IParticipants
    {
        public Task<List<Participant>> GetParticipants(string tournamentUrl);
        public Task<Participant> GetParticipantByGroupId(string tournamentUrl, int groupId);
        public Task<Participant> GetParticipantById(string tournamentUrl, int participantId);
    }
}
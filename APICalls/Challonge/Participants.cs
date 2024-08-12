using Challonge.Api;
using Challonge.Objects;

namespace BeybladeTournamentManager.ApiCalls.Challonge
{
    public class Participants : IParticipants
    {
        readonly IAutentication _auth;
        readonly ChallongeClient _client;

        public Participants(IAutentication auth)
        {
            _auth = auth;
            _client = _auth.GetClient();
        }

        public async Task<Participant> GetParticipantByGroupId(string tournamentUrl, int groupId)
        {
             return (await _client.GetParticipantsAsync(tournamentUrl)).ToList().FirstOrDefault(p => p.GroupPlayerIds.FirstOrDefault() == groupId);
        }

        public async Task<Participant> GetParticipantById(string tournamentUrl, int participantId)
        {
            return (await _client.GetParticipantsAsync(tournamentUrl)).ToList().FirstOrDefault(p => p.Id == participantId);
        }

        public async Task<List<Participant>> GetParticipants(string tournamentUrl)
        {
            return (await _client.GetParticipantsAsync(tournamentUrl)).ToList();
        }

        
    }
}
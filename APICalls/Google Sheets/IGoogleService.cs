using Google.Apis.Sheets.v4;

namespace BeybladeTournamentManager.ApiCalls.Google
{
    public interface IGoogleService
    {
        public SheetsService GetService();
    }
}